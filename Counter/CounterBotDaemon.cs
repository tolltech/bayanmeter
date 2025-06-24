using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.TelegramCore;

namespace Tolltech.Counter;

public class CounterBotDaemon(TelegramBotClient telegramBotClient, ITelegramClient telegramClient, ICounterService counterService)
    : IBotDaemon
{
    private readonly TelegramBotClient telegramBotClient = telegramBotClient;
    private readonly ITelegramClient telegramClient = telegramClient;
    private readonly ICounterService counterService = counterService;
    private static readonly ILog log = LogManager.GetLogger(typeof(CounterBotDaemon));

    public async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message)
            return;

        try
        {
            var message = update.Message;
            if (message == null)
            {
                return;
            }

            log.Info($"ReceiveMessage {message.Chat.Id} {message.MessageId}");

            var messageText = message.Text ?? string.Empty;

            if (!messageText.StartsWith("@")) return;

            var words = messageText.Split([" "], StringSplitOptions.RemoveEmptyEntries);
            if (words.Length != 2) return;

            var userName = words[0].Substring(1);
            if (string.IsNullOrWhiteSpace(userName)) return;
            if (!int.TryParse(words[1], out var number)) return;

            log.Info($"Increment {userName} {message.Chat.Id} by {number}");
            await counterService.Increment(userName, message.Chat.Id, number);
        }
        catch (Exception e)
        {
            log.Error("BotDaemonException", e);
            Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            if (update.Message?.Chat.Id != null)
                await client.SendTextMessageAsync(update.Message.Chat.Id, "Exception!",
                    cancellationToken: cancellationToken);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        log.Error("BotDaemonException", exception);
        Console.WriteLine($"BotDaemonException: {errorMessage} {exception.StackTrace}");
        return Task.CompletedTask;
    }
}