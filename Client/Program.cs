using System.Net.WebSockets;
using System.Text;

class Client
{
    static readonly ConsoleColor[] Palette = new[]
    {
        ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Green,
        ConsoleColor.Magenta, ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.Gray
    };

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "КЛІЄНТ (WebSocket)";

        Console.Write("Введіть нік: ");
        string nick = Console.ReadLine()?.Trim() ?? "user";
        if (string.IsNullOrWhiteSpace(nick)) nick = "user";

        int colorId = AskColorId();

        Console.Write("WS URL сервера (Enter = wss://<твій-сервіс>.onrender.com/ws): ");
        string? url = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(url))
            url = "wss://REPLACE.onrender.com/ws";

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(url), CancellationToken.None);

        // приём
        _ = Task.Run(async () =>
        {
            var buffer = new byte[8192];
            while (ws.State == WebSocketState.Open)
            {
                var sb = new StringBuilder();
                WebSocketReceiveResult res;
                do
                {
                    res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (res.MessageType == WebSocketMessageType.Close) return;
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, res.Count));
                } while (!res.EndOfMessage);

                var parts = sb.ToString().Split('|', 5);
                if (parts.Length == 5 && parts[0] == "CHAT")
                {
                    string time = parts[1];
                    string from = parts[2];
                    int fromColor = ParseColorId(parts[3]);
                    string text = parts[4];

                    var old = Console.ForegroundColor;
                    Console.ForegroundColor = ColorFromId(fromColor);
                    Console.WriteLine($"[{time}] {from}: {text}");
                    Console.ForegroundColor = old;
                }
            }
        });

        Console.WriteLine("Пишіть повідомлення. /exit — вихід.");
        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line == null) continue;
            if (line.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;

            line = line.Replace("|", "/"); // чтобы не ломать протокол
            string payload = $"{nick}|{colorId}|{line}";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        if (ws.State == WebSocketState.Open)
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
    }

    static int AskColorId()
    {
        Console.WriteLine("Оберіть колір (введіть номер):");
        Console.WriteLine(" 1) White\n 2) Yellow\n 3) Cyan\n 4) Green\n 5) Magenta\n 6) Blue\n 7) Red\n 8) Gray");
        Console.Write("Ваш вибір [1..8] (Enter = 1): ");
        var s = Console.ReadLine();
        return (int.TryParse(s, out int n) && n >= 1 && n <= 8) ? n : 1;
    }

    static int ParseColorId(string token)
    {
        if (int.TryParse(token, out int n) && n >= 1 && n <= 8) return n;
        if (Enum.TryParse<ConsoleColor>(token, true, out var cc))
            for (int i = 0; i < Palette.Length; i++) if (Palette[i] == cc) return i + 1;
        return 1;
    }

    static ConsoleColor ColorFromId(int id)
    {
        int idx = Math.Clamp(id, 1, 8) - 1;
        return Palette[idx];
    }
}
