using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.TelegramCore;

namespace Tolltech.LevDimover;

public class LevDimovBotDaemon : IBotDaemon
{
    private static readonly ILog log = LogManager.GetLogger(typeof(LevDimovBotDaemon));

    private static string GetLevPath()
    {
        var paths = Directory.GetFiles("leves");
        return paths.OrderBy(x => Guid.NewGuid()).FirstOrDefault() ?? string.Empty;
    }

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

            if (messageText.ToLower() == "лев димов"
                || messageText.ToLower() == "лева")
            {
                var path = GetLevPath();

                if (string.IsNullOrWhiteSpace(path))
                {
                    log.Info($"No files to send");
                    return;
                }

                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                await client.SendPhoto(message.Chat.Id, new InputFileStream(stream),
                    cancellationToken: cancellationToken,
                    replyParameters: new ReplyParameters { MessageId = message.MessageId } );
                return;
            }

            if (messageText.Contains("instagram.com") && !messageText.Contains("ddinstagram.com")
                || (messageText.Contains("instagram.com") && !messageText.Contains("kkinstagram.com")))
            {
                await client.SendMessage(message.Chat.Id,
                    messageText.Replace("instagram.com", "kkinstagram.com"),
                    cancellationToken: cancellationToken,
                    replyParameters: new ReplyParameters { MessageId = message.MessageId });
                return;
            }

            var replyMessageText = LevDimovService.Convert(messageText);

            log.Info($"GetNewMessage {replyMessageText} from {messageText}");

            if (messageText != replyMessageText)
            {
                await client.SendMessage(message.Chat.Id, replyMessageText,
                    cancellationToken: cancellationToken,
                    replyParameters: new ReplyParameters { MessageId = message.MessageId });
            }
        }
        catch (Exception e)
        {
            log.Error("BotDaemonException", e);
            Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            if (update.Message?.Chat.Id != null)
                await client.SendMessage(update.Message.Chat.Id, "Exception!",
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