using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server
{
    static int GetPort() => int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 9000;

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "СЕРВЕР (UDP)";
        int port = GetPort();

        using var server = new UdpClient(port);
        server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        var clients = new List<IPEndPoint>();
        Console.WriteLine($"Сервер слухає UDP {port} (0.0.0.0:{port})");

        while (true)
        {
            UdpReceiveResult res;
            try { res = await server.ReceiveAsync(); }
            catch (Exception ex)
            {
                Console.WriteLine("Receive error: " + ex.Message);
                continue;
            }

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

            foreach (var c in clients.ToArray())
            {
                try
                {
                    await server.SendAsync(data, data.Length, c);
                }
                catch
                {
                   
                }
            }

            Console.WriteLine($"[{time}] {nick}: {text}");
        }
    }
}
