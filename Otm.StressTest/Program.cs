namespace Otm.StressTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var server = new PlcServer(7099);
            server.Start();

            // wait for escape key
            Console.WriteLine("Press ESC to stop server");
            while (Console.ReadKey().Key != ConsoleKey.Escape) { await Task.Delay(100); }
        }
    }
}