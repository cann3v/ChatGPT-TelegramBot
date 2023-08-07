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
            XmlConfigurator.Configure(new System.IO.FileInfo("{PATH TO log4net.config}"));

            OpenAIAPI api = new OpenAIAPI(APIAuthentication.LoadFromPath());
            string token = File.ReadAllText("{PATH TO TELEGRAM BOT TOKEN FILE}");
            
            log.Warn("All tokens load successfully.");

            Database db = new Database("{PATH TO DATABASE}");
            db.CreateDatabaseFile();
            db.CreateTables();
            
            Bot bot = new Bot(token, api);
            Console.ReadLine();
        }
    }
}
