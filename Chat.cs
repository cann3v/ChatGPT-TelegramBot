using OpenAI_API;
using OpenAI_API.Chat;

namespace chatgpt_bot;

public class Chat
{
    private OpenAIAPI _api;
    private Conversation _chat;
    private int _msgCount;
    
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
}
