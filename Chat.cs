using OpenAI_API;
using OpenAI_API.Chat;
using log4net;

namespace chatgpt_bot;

[Serializable]
public class Chat
{
    private OpenAIAPI _api;
    private Conversation _chat;
    private int _msgCount;
    private static readonly ILog Log = LogManager.GetLogger(typeof(Chat));
    
    public Chat(OpenAIAPI api)
    {
        _api = api;
        CreateConversation();
    }

    public void CreateConversation()
    {
        _chat = _api.Chat.CreateConversation();
        _msgCount = 0;
    }

    public async IAsyncEnumerable<string> SendMessage(string userInput)
    {
        if (_msgCount >= 50)
            CreateConversation();
        
        _chat.AppendUserInput(userInput);
        _msgCount += 1;
        await foreach (var res in _chat.StreamResponseEnumerableFromChatbotAsync())
        {
            yield return res;
        }
    }

    public void AppendChatHistory(List<List<string>> chatHistory)
    {
        List<string> user = chatHistory[0];
        List<string> ai = chatHistory[1];

        if (user.Count != ai.Count)
            Log.Warn($"user.Count({user.Count}) != ai.Count({ai.Count})");

        for (int i = 0; i < ai.Count; i++)
        {
            _chat.AppendUserInput(user[i]);
            _chat.AppendExampleChatbotOutput(ai[i]);
        }
    }
}
