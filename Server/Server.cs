// Application entry point that launches the ASP.NET Core ELib server.
using Server.Configuration;

namespace Server
{
    public class Server
    {
        public static void Main(string[] args)
        {
            try
            {
                var app = Config.Configure(args);
                app.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("=== FATAL STARTUP ERROR ===");
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
