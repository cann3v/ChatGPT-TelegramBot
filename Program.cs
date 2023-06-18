using OpenAI_API;
using log4net;
using log4net.Config;

namespace chatgpt_bot
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        
        private static void Main(string[] args)
        {
            XmlConfigurator.Configure(new System.IO.FileInfo("R:\\Roman\\aaa\\csharp\\chatgpt-bot\\log4net.config"));

            OpenAIAPI api = new OpenAIAPI(APIAuthentication.LoadFromPath());
            string token = File.ReadAllText("R:\\Roman\\aaa\\csharp\\chatgpt-bot\\.telegrambot");
            
            log.Warn("All tokens load successfully.");

            Database db = new Database("R:\\Roman\\aaa\\csharp\\chatgpt-bot\\users.db");
            db.CreateDatabaseFile();
            db.CreateTable();
            
            Bot bot = new Bot(token, api);
            Console.ReadLine();
        }
    }
}
