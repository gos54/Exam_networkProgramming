using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class VotingServer
{
    private static Dictionary<int, int> votes = new Dictionary<int, int>();
    private static List<string> options = new List<string> { "Option 1", "Option 2", "Option 3" };
    private static List<string> admin_usernames = new List<string> { "Admin" };
    private static Dictionary<string, int> user_votes = new Dictionary<string, int>();
    private static Timer voting_timer;
    private static bool voting_open = true;
    private static List<TcpClient> clients = new List<TcpClient>();

    static void Main(string[] args)
    {
        int minuts = 5;
        TcpListener server = new TcpListener(IPAddress.Any, 8888);
        server.Start();
        Console.WriteLine("Server started.");

        voting_timer = new Timer(EndVoting, null, minuts * 60000, Timeout.Infinite);

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            lock (clients) {  clients.Add(client); }
            Thread client_thread = new Thread(HandleClient);
            client_thread.Start(client);
        }
    }

    private static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int read_bytes;

        try
        {
            string options_message = "Available options:\n";
            for (int i = 0; i < options.Count; i++) { options_message += $"{i + 1}. {options[i]}\n"; }
            options_message += "To vote, enter the command \"/vote option_number\"\nTo exit, enter the command \"/exit\"";

            byte[] options_data = Encoding.ASCII.GetBytes(options_message);
            stream.Write(options_data, 0, options_data.Length);

            while ((read_bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                string message = Encoding.ASCII.GetString(buffer, 0, read_bytes);
                string[] parts = message.Split('^');
                string username = parts[1];
                Console.WriteLine(message);
                if (message.StartsWith("/exit")) 
                {
                    Console.WriteLine($"Client {username} disconect.");
                }
                else if (message.StartsWith("/vote"))
                {
                    if (parts[2].All(char.IsDigit))
                    {
                        int vote = Convert.ToInt32(parts[2]);
                        if (voting_open && (0 < vote && vote <= options.Count))
                        {
                            lock (user_votes)
                            {
                                if (user_votes.ContainsKey(username))
                                {
                                    int previous = user_votes[username];
                                    votes[previous]--;
                                }
                                user_votes[username] = vote;
                                if (votes.ContainsKey(vote))
                                {
                                    votes[vote]++;
                                }
                                else
                                {
                                    votes[vote] = 1;
                                }
                            }
                            BroadcastResults("Results:\n");
                        }
                    }
                }
                else if (message.StartsWith("/shutdown") && admin_usernames.Contains(username))
                {
                    Environment.Exit(0);
                }
                else if (message.StartsWith("/add") && admin_usernames.Contains(username))
                {
                    options.Add(parts[2]);
                    options_message = "Available options:\n";
                    for (int i = 0; i < options.Count; i++) { options_message += $"{i + 1}. {options[i]}\n"; }

                    options_data = Encoding.ASCII.GetBytes(options_message);
                    stream.Write(options_data, 0, options_data.Length);
                }
                else if (message.StartsWith("/remove") && admin_usernames.Contains(username))
                {
                    if (options.Contains(parts[2])) { options.Remove(parts[2]); }
                    options_message = "Available options:\n";
                    for (int i = 0; i < options.Count; i++) { options_message += $"{i + 1}. {options[i]}\n"; }

                    options_data = Encoding.ASCII.GetBytes(options_message);
                    stream.Write(options_data, 0, options_data.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client disconnected: {ex.Message}");
        }
        finally
        {
            lock (clients)  { clients.Remove(client); }
            client.Close();
        }
    }

    private static void BroadcastResults(string dop)
    {
        StringBuilder results = new StringBuilder(dop);
        for (int i = 0; i < options.Count; i++)
        {
            results.AppendLine($"{options[i]}: {votes.GetValueOrDefault(i + 1, 0)} votes");
        }

        byte[] data = Encoding.ASCII.GetBytes(results.ToString());
        lock (clients) 
        {
            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send data to client: {ex.Message}");
                }
            }
        }
    }

    private static void EndVoting(object state)
    {
        voting_open = false;
        BroadcastResults("Final results:\n");
        Console.WriteLine("Voting has ended.");
    }
}