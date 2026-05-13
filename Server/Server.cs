// Application entry point that launches the ASP.NET Core ELib server.
using Server.Configuration;

namespace Server
{
    public class Server
    {
        public static void Main(string[] args)
        {
            var app = Config.Configure(args);
            app.Run();
        }
    }
}
