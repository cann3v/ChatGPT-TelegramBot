using System.Diagnostics;
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
    private static readonly int _maxMessages = 50;
    private Random _random = new Random();
    private const double DefaultMaxTokens = 3500;

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
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        Log.Debug($"Received '{messageText}' from {update.Message.From.Username} ({update.Message.From.Id})");

        if (messageText.StartsWith("/"))
            await HandleCommand(botClient, update, cancellationToken);
        else
            await HandleMessage(botClient, update, cancellationToken);
    }

    private async Task HandleMessage(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: update.Message.Chat.Id,
            text: "[ChatGPT]: ",
            cancellationToken: cancellationToken);

        double maxTokens = DefaultMaxTokens;
        while (true)
        {
            try
            {
                await GenerateResponse(botClient, update, cancellationToken, sentMessage.MessageId, (int)maxTokens);
                break;
            }
            catch (HttpRequestException)
            {
                maxTokens *= 0.9;
                Log.Info($"Caught HttpRequestException. Reducing maximum number of tokens to {(int)maxTokens}. " +
                          $"User: {update.Message.From.Id}({update.Message.From.Username})");
            }
        }
    }

    private async Task GenerateResponse(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken, int msgId, int maxTokens)
    {
        Chat cht = new Chat(_oaitoken);
        cht.UserInput = update.Message.Text;
        cht.MaxTokens = maxTokens;
        cht.CreateConversation();
        string resp = string.Empty;
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
        
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        await foreach (var res in cht.SendMessage(update.Message.Text))
        {
            resp += res;

            if (stopwatch.ElapsedMilliseconds >= _random.Next(1000, 2001))
            {
                await botClient.EditMessageTextAsync(
                    chatId: update.Message.Chat.Id,
                    messageId: msgId,
                    text: resp,
                    cancellationToken: cancellationToken);
                stopwatch.Restart();
            }
        }
        await botClient.EditMessageTextAsync(
            chatId: update.Message.Chat.Id,
            messageId: msgId,
            text: resp,
            cancellationToken: cancellationToken);
        
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
                await _db.ClearChats(update.Message.From.Id);
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "Message history cleared!",
                    cancellationToken: cancellationToken);
                break;
            case "/about":
                string strAbout =
                    "A Telegram bot with fully open-source code, written in C#, " +
                    "allowing communication with gpt-3.5-turbo. Creator: @cannev.";
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: strAbout,
                    cancellationToken: cancellationToken);
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

    private List<List<string>>? Deserialize(string? json)
    {
        return JsonSerializer.Deserialize<List<List<string>>>(json);
    }

    private async Task SaveChat(long userId, List<List<string>> chat)
    {
        if (chat[0].Count != chat[1].Count)
        {
            Log.Warn($"User inputs != ai outputs. Clearing chat history");
            await _db.ClearChats(userId);
            return;
        }

        while (chat[0].Count >= _maxMessages)
        {
            Log.Debug($"User {userId} has more than {_maxMessages} messages");
            chat[0].RemoveAt(0);
            chat[1].RemoveAt(0);
        }
        string serializedChat = Serialize(chat);
        await _db.InsertChats(userId, serializedChat);
    }

    private async Task<List<List<string>>?> LoadChat(long userId)
    {
        string? serializedChat = await _db.SelectChats(userId);
        List<List<string>>? chat = Deserialize(serializedChat);
        return chat;
    }
}
