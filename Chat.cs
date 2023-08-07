using OpenAI_API;
using OpenAI_API.Chat;
using log4net;
using SharpToken;
using Model = OpenAI_API.Models.Model;

namespace chatgpt_bot;

[Serializable]
public class Chat
{
    private OpenAIAPI _api;
    private Conversation _chat;
    private static readonly ILog Log = LogManager.GetLogger(typeof(Chat));
    private int _maxTokens = 4096;
    private string _userInput;
    private int _messageTokens;

    public Chat(OpenAIAPI api)
    {
        _api = api;
    }

    public string UserInput
    {
        get => _userInput;
        set => _userInput = value;
    }

    public int MaxTokens
    {
        get => _maxTokens;
        set => _maxTokens = value;
    }
    
    public void CreateConversation()
    {
        CalculateTokens();
        _chat = _api.Chat.CreateConversation(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            MaxTokens = _maxTokens - _messageTokens
        });
    }

    private void CalculateTokens()
    {
        var encoding = GptEncoding.GetEncoding("cl100k_base");
        var encoded = encoding.Encode(_userInput);
        _messageTokens = encoded.Count;
    }

    public async IAsyncEnumerable<string> SendMessage(string userInput)
    {
        _chat.AppendUserInput(userInput);
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
