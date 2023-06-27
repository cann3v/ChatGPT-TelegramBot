using System.Text.Json;
using log4net;
using OpenAI_API;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace chatgpt_bot;

public class Bot
{
    private OpenAIAPI _oaitoken;
    private TelegramBotClient _botClient;
    private ReceiverOptions _receiverOptions;
    private CancellationTokenSource _cts;
    private static readonly ILog Log = LogManager.GetLogger(typeof(Bot));
    private Database _db = new Database("R:\\Roman\\aaa\\csharp\\chatgpt-bot\\users.db");

    public Bot(string token, OpenAIAPI oaitoken)
    {
        _oaitoken = oaitoken;
        _cts = new();
        _botClient = new TelegramBotClient(token);
        _receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true
        };
        Start();
    }

    private async void Start()
    {
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: _receiverOptions,
            cancellationToken: _cts.Token
        );
        var me = await _botClient.GetMeAsync();
        Log.Warn($"Start listening for @{me.Username}");
    }

    // TODO
    // Очищать сообщения в Chat каждые 50 раз
    // Разобраться с задержкой при редактировании собщения
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        
        Log.Debug($"Received a '{messageText}' message in chat {chatId}.");

        if (messageText.StartsWith("/"))
            await HandleCommand(botClient, update, cancellationToken);
        else
            await HandleMessage(botClient, update, cancellationToken);
    }
    
    private async Task HandleMessage(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        Chat cht = new Chat(_oaitoken);
        string resp = "";
        List<List<string>>? chatHistory;
        
        try
        {
            chatHistory = await LoadChat(update.Message.Chat.Id);
            cht.AppendChatHistory(chatHistory);
        }
        catch (ArgumentNullException)
        {
            chatHistory = new List<List<string>>();
        }

        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "[ChatGPT]: ",
            cancellationToken: cancellationToken);
        await foreach (var res in cht.SendMessage(update.Message.Text))
        {
            resp += res;
            await botClient.EditMessageTextAsync(
                chatId: update.Message.Chat.Id,
                messageId: sentMessage.MessageId,
                text: resp,
                cancellationToken: cancellationToken);
            //await Task.Delay(500);
        }

        if (chatHistory.Count != 0)
        {
            chatHistory[0].Add(update.Message.Text);
            chatHistory[1].Add(resp);
        }
        else
        {
            chatHistory.Add(new List<string>() {update.Message.Text});
            chatHistory.Add(new List<string>() { resp });
        }
        await SaveChat(update.Message.Chat.Id, chatHistory);
        Log.Debug($"Response from ChatGPT: {resp}");
    }

    private async Task HandleCommand(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        switch (update.Message.Text)
        {
            case "/start":
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "Hello!",
                    cancellationToken: cancellationToken);
                await _db.InsertUsers(update.Message.From.Id, update.Message.From.Username);
                break;
            case "/clear":
                break;
            default:
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "Unknown command",
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:[{apiRequestException.ErrorCode}] - {apiRequestException.Message}",
            _ => exception.ToString()
        };

        Log.Error(ErrorMessage);
        return Task.CompletedTask;
    }

    private string Serialize(List<List<string>> data)
    {
        return JsonSerializer.Serialize(data);
    }

    private List<List<string>>? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<List<List<string>>>(json);
    }

    private async Task SaveChat(long userId, List<List<string>> chat)
    {
        string serializedChat = Serialize(chat);
        Log.Debug($"Serialized chat: {serializedChat}");
        await _db.InsertChats(userId, serializedChat);
    }

    private async Task<List<List<string>>?> LoadChat(long userId)
    {
        string? serializedChat = await _db.SelectChats(userId);
        List<List<string>>? chat = Deserialize(serializedChat);
        return chat;
    }
}
