using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;

namespace CS2Cheat;

public class Program
{
    private static void W(string text, ConsoleColor c = ConsoleColor.Gray, bool nl = true)
    {
        Console.ForegroundColor = c;
        if (nl) Console.WriteLine(text); else Console.Write(text);
        Console.ResetColor();
    }

    private static void Logo()
    {
        void P(string t) { Console.CursorLeft = (60 - t.Length) / 2; W(t, ConsoleColor.Cyan); }
        Console.WriteLine();
        P("┌──────────────────────────┐");
        P("│   M E M A T I H A C K    │");
        P("│     E X T E R N A L      │");
        P("│    C S 2   v 2 . 0       │");
        P("└──────────────────────────┘");
    }

    private static void Step(int num, string msg)
    {
        Console.Write("  ");
        W($"◆", ConsoleColor.Cyan, false);
        W($"  {msg}", ConsoleColor.Gray);
    }

    private static void Done(string msg)
    {
        Console.CursorLeft = 4;
        W("✔  " + msg, ConsoleColor.Green);
    }

    public static async Task Main()
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "MematiHack v2.0";
            Console.CursorVisible = false;
            try { Console.SetWindowSize(64, 26); Console.SetBufferSize(64, 26); } catch { }

            Logo();
            W("  ──────────────────────────────────────────────────────────", ConsoleColor.DarkGray);

            Step(1, "Offsetler güncelleniyor...");
            await Offsets.UpdateOffsets();
            Done("Offsetler güncellendi");

            Step(2, "CS2 işlemi bekleniyor...");
            var gameProcess = new GameProcess();
            gameProcess.Start();

            var spin = new[] { '|', '/', '-', '\\' };
            int si = 0, dt = 0;
            while (!gameProcess.IsValid || gameProcess.WindowRectangleClient.Width <= 0)
            {
                Console.CursorLeft = 32;
                W($"{spin[si++ % 4]}  Bekleniyor{new string('.', (dt++ / 8) % 4)}   ", ConsoleColor.DarkYellow, false);
                await Task.Delay(200);
            }
            var res = $"{gameProcess.WindowRectangleClient.Width}x{gameProcess.WindowRectangleClient.Height}";
            Console.CursorLeft = 4;
            W("✔  CS2 bulundu", ConsoleColor.Green, false);
            W($"  ({res})", ConsoleColor.DarkGray);

            Step(3, "Oyun verisi başlatılıyor...");
            var gameData = new GameData(gameProcess);
            gameData.Start();

            for (int p = 0; p <= 100; p += 4)
            {
                Console.CursorLeft = 32;
                var bar = new string('■', p / 4) + new string('□', 25 - p / 4);
                W($"[{bar}] {p}%", ConsoleColor.DarkGray, false);
                await Task.Delay(20);
            }
            Done("Oyun verisi hazır");

            Step(4, "Overlay başlatılıyor...");
            var overlay = new OverlayRenderer(gameProcess, gameData);
            overlay.StartFeatures();
            Done("Overlay başlatıldı");

            W("  ──────────────────────────────────────────────────────────", ConsoleColor.DarkGray);
            Console.WriteLine();
            void C(string t) { Console.CursorLeft = (60 - t.Length) / 2; W(t, ConsoleColor.Cyan); }
            void M(string t) { Console.CursorLeft = (60 - t.Length) / 2; W(t, ConsoleColor.DarkGray); }
            C("EXTERNAL CHEAT AKTİF");
            M("────────────────────");
            M("Insert / RShift = Menü");
            M("End = Kaydet & Kapat");
            M("F6 = Acil Çıkış");
            Console.WriteLine();
            Console.CursorLeft = (60 - 22) / 2;
            W("[ KONSOLU KAPATMAYIN ]", ConsoleColor.DarkYellow);

            await overlay.Run();

            overlay.StopFeatures();
            gameData.Dispose();
            gameProcess.Dispose();
        }
        catch (Exception ex)
        {
            Console.Clear();
            W(" [!] KRİTİK HATA OLUŞTU [!]", ConsoleColor.Red);
            W("──────────────────────────────────────────────────────────", ConsoleColor.DarkGray);
            W($"Hata Mesajı: {ex.Message}", ConsoleColor.Yellow);
            W("\nDetaylar:", ConsoleColor.Gray);
            W(ex.StackTrace ?? "Detay yok.", ConsoleColor.DarkGray);
            W("\n──────────────────────────────────────────────────────────", ConsoleColor.DarkGray);
            W("Kapatmak için bir tuşa basın...", ConsoleColor.Cyan);
            Console.ReadKey();
        }
    }
}
