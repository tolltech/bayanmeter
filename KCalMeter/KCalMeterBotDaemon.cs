using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.SqlEF;
using Tolltech.TelegramCore;

namespace KCalMeter;

public class KCalMeterBotDaemon : IBotDaemon
{
    private readonly IQueryExecutorFactory queryExecutorFactory;
    private readonly TelegramBotClient telegramBotClient;
    private readonly ITelegramClient telegramClient;
    private static readonly ILog log = LogManager.GetLogger(typeof(KCalMeterBotDaemon));

    public KCalMeterBotDaemon(IQueryExecutorFactory queryExecutorFactory, TelegramBotClient telegramBotClient,
        ITelegramClient telegramClient)
    {
        this.queryExecutorFactory = queryExecutorFactory;
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

           // await ParseAndSaveHistory(client, cancellationToken, message).ConfigureAwait(false);

           var args = message.Text?
                          .Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(x => x.Trim())
                          .ToArray()
                      ?? Array.Empty<string>();

           var command = args.FirstOrDefault();
           var firstArg = args.Skip(1).FirstOrDefault();
           var secondArg = args.Skip(2).FirstOrDefault();
        }
        catch (Exception e)
        {
            log.Error("BotDaemonException", e);
            Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            if (update.Message?.Chat.Id != null)
                await client
                    .SendTextMessageAsync(update.Message.Chat.Id, "Exception!",
                        cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        log.Error("BotDaemonException", exception);
        Console.WriteLine($"BotDaemonException: {ErrorMessage} {exception.StackTrace}");
        return Task.CompletedTask;
    }
}