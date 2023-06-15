using OpenAI_API;
using log4net;
using log4net.Config;

namespace chatgpt_bot
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        
        private static async Task Main(string[] args)
        {
            XmlConfigurator.Configure(new System.IO.FileInfo("R:\\Roman\\aaa\\csharp\\chatgpt-bot\\log4net.config"));
            
            /*OpenAIAPI api = new OpenAIAPI(APIAuthentication.LoadFromPath());

            Chat cht = new Chat(api);
            await foreach (var res in cht.SendMessage("Hello! My name is Heisenberg."))
            {
                Console.Write(res);
            }
            Console.WriteLine();
            await foreach (var res in cht.SendMessage("SAY MY NAME."))
            {
                Console.Write(res);
            }*/

            string token = File.ReadAllText("R:\\Roman\\aaa\\csharp\\chatgpt-bot\\.telegrambot");
            Bot bot = new Bot(token);
            Console.ReadLine();
        }
    }
}
