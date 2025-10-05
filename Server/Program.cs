using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server
{
    const int Port = 9000;

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "СЕРВЕР";
        using var server = new UdpClient(Port);
        var clients = new List<IPEndPoint>();
        Console.WriteLine($"Сервер слухає UDP {Port}");

        while (true)
        {
            var res = await server.ReceiveAsync();
            var remote = res.RemoteEndPoint;

            if (!clients.Exists(ep => ep.Equals(remote)))
                clients.Add(remote);

            string payload = Encoding.UTF8.GetString(res.Buffer);
            var parts = payload.Split('|', 3);
            if (parts.Length < 3) continue;

            string nick = parts[0];
            string colorId = parts[1];
            string text = parts[2];

            string time = DateTime.Now.ToString("HH:mm:ss");
            string outPacket = $"CHAT|{time}|{nick}|{colorId}|{text}";
            byte[] data = Encoding.UTF8.GetBytes(outPacket);

            foreach (var c in clients)
                await server.SendAsync(data, data.Length, c);

            Console.WriteLine($"[{time}] {nick}: {text}");
        }
    }
}
