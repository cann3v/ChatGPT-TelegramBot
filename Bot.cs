using System.Text;
using log4net;
using OpenAI_API;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace chatgpt_bot;

public class Bot
{
    private OpenAIAPI _oaitoken;
    private TelegramBotClient _botClient;
    private ReceiverOptions _receiverOptions;
    private CancellationTokenSource _cts;
    private static readonly ILog log = LogManager.GetLogger(typeof(Bot));

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
        log.Warn($"Start listening for @{me.Username}");
    }

    // TODO
    // Интегрировать базу данных
    // Сериализация объектов класса Chat 
    // Очищать сообщения в Chat каждые 50 раз
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        
        log.Debug($"Received a '{messageText}' message in chat {chatId}.");

        if (messageText.StartsWith("/"))
            await HandleCommand(botClient, update, cancellationToken);
        else
        {
            Chat cht = new Chat(_oaitoken);
            string resp = "[ChatGPT]: ";
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: resp,
                cancellationToken: cancellationToken);
            await foreach (var res in cht.SendMessage(messageText))
            {
                resp += res;
                botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: sentMessage.MessageId,
                    text: resp);
                await Task.Delay(500);
            }
            log.Debug($"Responde from ChatGPT: {resp}");
        }
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

        log.Error(ErrorMessage);
        return Task.CompletedTask;
    }
}
