using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.TelegramCore;

namespace Tolltech.LevDimover;

public class LevDimovBotDaemon : IBotDaemon
{
    private readonly TelegramBotClient telegramBotClient;
    private readonly ITelegramClient telegramClient;
    private static readonly ILog log = LogManager.GetLogger(typeof(LevDimovBotDaemon));

    public LevDimovBotDaemon(TelegramBotClient telegramBotClient, ITelegramClient telegramClient)
    {
        this.telegramBotClient = telegramBotClient;
        this.telegramClient = telegramClient;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message)
            return;

        try
        {
            var message = update.Message;
            if (message == null || message.ForwardDate.HasValue || message.ReplyToMessage != null)
            {
                return;
            }

            log.Info($"ReceiveMessage {message.Chat.Id} {message.MessageId}");

            var messageText = message.Text ?? string.Empty;
            var replyMessageText = LevDimovService.Convert(messageText);
            
            log.Info($"GetNewMessage {replyMessageText} from {messageText}");
            
            if (messageText != replyMessageText)
            {
                await client.SendTextMessageAsync(message.Chat.Id, replyMessageText,
                    cancellationToken: cancellationToken,
                    replyToMessageId: message.MessageId);   
            }
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