using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Client
{
    const int ServerPort = 9000;
    static readonly ConsoleColor[] Palette = new[]
    {
        ConsoleColor.White,
        ConsoleColor.Yellow,
        ConsoleColor.Cyan,
        ConsoleColor.Green,
        ConsoleColor.Magenta,
        ConsoleColor.Blue,
        ConsoleColor.Red,
        ConsoleColor.Gray
    };

    static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "КЛІЄНТ";

        Console.Write("Введіть нік: ");
        string nick = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(nick)) nick = "user";

        int colorId = AskColorId();

        var serverEp = new IPEndPoint(IPAddress.Loopback, ServerPort);
        using var client = new UdpClient();
        client.Connect(serverEp);

        _ = Task.Run(async () =>
        {
            while (true)
            {
                UdpReceiveResult res;
                try { res = await client.ReceiveAsync(); }
                catch { break; }

                var parts = Encoding.UTF8.GetString(res.Buffer).Split('|', 5);
                if (parts.Length < 5 || parts[0] != "CHAT") continue;

                string time = parts[1];
                string fromNick = parts[2];
                int fromColorId = ParseColorId(parts[3]);
                string text = parts[4];

                var old = Console.ForegroundColor;
                Console.ForegroundColor = ColorFromId(fromColorId);
                Console.WriteLine($"[{time}] {fromNick}: {text}");
                Console.ForegroundColor = old;
            }
        });

        Console.WriteLine("Пишіть повідомлення. /exit — вихід.");
        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (line == null) continue;
            if (line.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;

            line = line.Replace("|", "/");

            string payload = $"{nick}|{colorId}|{line}";
            byte[] data = Encoding.UTF8.GetBytes(payload);
            await client.SendAsync(data, data.Length);
        }
    }

    static int AskColorId()
    {
        Console.WriteLine("Оберіть колір (введіть номер):");
        Console.WriteLine(" 1) White");
        Console.WriteLine(" 2) Yellow");
        Console.WriteLine(" 3) Cyan");
        Console.WriteLine(" 4) Green");
        Console.WriteLine(" 5) Magenta");
        Console.WriteLine(" 6) Blue");
        Console.WriteLine(" 7) Red");
        Console.WriteLine(" 8) Gray");
        Console.Write("Ваш вибір [1..8] (Enter = 1): ");

        var s = Console.ReadLine();
        if (int.TryParse(s, out int n) && n >= 1 && n <= 8) return n;
        return 1;
    }

    static int ParseColorId(string token)
    {
        if (int.TryParse(token, out int n) && n >= 1 && n <= 8) return n;

        if (Enum.TryParse<ConsoleColor>(token, true, out var cc))
        {
            for (int i = 0; i < Palette.Length; i++)
                if (Palette[i] == cc) return i + 1;
        }
        return 1;
    }
    static ConsoleColor ColorFromId(int id)
    {
        int idx = Math.Clamp(id, 1, 8) - 1;
        return Palette[idx];
    }
}
