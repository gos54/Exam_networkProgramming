using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class VotingClient
{
    static void Main(string[] args)
    {
        TcpClient client = new TcpClient("127.0.0.1", 8888);
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        Console.Write("Enter your username: ");
        string username = Console.ReadLine();

        bytesRead = stream.Read(buffer, 0, buffer.Length);
        string optionsMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine(optionsMessage);

        Thread receiveThread = new Thread(() =>
        {
            while (true)
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine(message);
            }
        });
        receiveThread.Start();

        while (true)
        {
            string input = Console.ReadLine();
            if (input == "/exit")
            {
                byte[] data = Encoding.ASCII.GetBytes($"/exit^{username}^123");
                stream.Write(data, 0, data.Length);
                break;
            }
            else if (input.StartsWith("/vote"))
            {
                byte[] data = Encoding.ASCII.GetBytes($"/vote^{username}^{string.Join(" ", input.Split(' ').Skip(1))}");
                stream.Write(data, 0, data.Length);
            }
            else if (input.StartsWith("/add"))
            {
                byte[] data = Encoding.ASCII.GetBytes($"/add^{username}^{string.Join(" ", input.Split(' ').Skip(1))}");
                stream.Write(data, 0, data.Length);
            }
            else if (input.StartsWith("/remove"))
            {
                byte[] data = Encoding.ASCII.GetBytes($"/remove^{username}^{string.Join(" ", input.Split(' ').Skip(1))}");
                stream.Write(data, 0, data.Length);
            }
            else if (input.StartsWith("/shutdown"))
            {
                byte[] data = Encoding.ASCII.GetBytes($"/shutdown^{username}^123");
                stream.Write(data, 0, data.Length);
            }
        }
        client.Close();
}
    }