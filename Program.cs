using OpenAI_API;
namespace chatgpt_bot
{
    class Program
    {
        private static async Task Main(string[] args)
        {
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

            string token = File.ReadAllText("../../../.telegrambot");
            Bot bot = new Bot(token);
            Console.ReadLine();
        }
    }
}
