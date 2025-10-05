using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

var clients = new ConcurrentDictionary<WebSocket, byte>();

app.MapGet("/", () => "UDP-chat relay over WebSocket. Connect to /ws via WebSocket.");

app.Map("/ws", async ctx =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsync("Expected WebSocket");
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    clients.TryAdd(ws, 1);

    var buf = new byte[8192];

    try
    {
        while (true)
        {
            var res = await ws.ReceiveAsync(buf, CancellationToken.None);
            if (res.MessageType == WebSocketMessageType.Close) break;
            if (res.MessageType != WebSocketMessageType.Text) continue;

            string payload = Encoding.UTF8.GetString(buf, 0, res.Count);
            var parts = payload.Split('|', 3);
            if (parts.Length < 3) continue;

            string nick = parts[0];
            string colorId = parts[1];
            string text = parts[2];

            string time = DateTime.Now.ToString("HH:mm:ss");
            string outPacket = $"CHAT|{time}|{nick}|{colorId}|{text}";
            byte[] data = Encoding.UTF8.GetBytes(outPacket);

            foreach (var socket in clients.Keys.ToArray())
            {
                if (socket.State != WebSocketState.Open)
                {
                    clients.TryRemove(socket, out _);
                    continue;
                }

                try
                {
                    await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch
                {
                    clients.TryRemove(socket, out _);
                }
            }

            Console.WriteLine($"[{time}] {nick}: {text}");
        }
    }
    finally
    {
        clients.TryRemove(ws, out _);
        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); } catch { }
    }
});

app.Run();
