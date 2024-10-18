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
    private readonly IKCalMeterService kCalMeterService;
    private static readonly ILog log = LogManager.GetLogger(typeof(KCalMeterBotDaemon));

    public KCalMeterBotDaemon(IQueryExecutorFactory queryExecutorFactory, TelegramBotClient telegramBotClient,
        ITelegramClient telegramClient, IKCalMeterService kCalMeterService)
    {
        this.queryExecutorFactory = queryExecutorFactory;
        this.telegramBotClient = telegramBotClient;
        this.telegramClient = telegramClient;
        this.kCalMeterService = kCalMeterService;
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

           var messageText = message.Text ?? string.Empty;

           if (messageText.StartsWith("/"))
           {
               await ProcessCommand(message);
               return;
           }
           
           var args = message.Text?
                          .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(x => x.Trim())
                          .ToArray()
                      ?? Array.Empty<string>();

           if (args.Length <= 2 && args.Length >= 1)
           {
               var name = args[0];
               if (!int.TryParse(args.Skip(1).FirstOrDefault(), out var portion))
               {
                   await SendError("Wrong format", client, message, cancellationToken);
                   return;
               }

               if (portion == 0) portion = 1;

               await kCalMeterService.WritePortion(name, portion, message.Chat.Id, message.From!.Id, message.Date);
           }
           else if (args.Length >= 5)
           {
               var name = args[0];
               
               if (!int.TryParse(args[1], out var kcal))
               {
                   await SendError("Wrong format", client, message, cancellationToken);
                   return;
               }
               
               if (!int.TryParse(args[2], out var protein))
               {
                   await SendError("Wrong format", client, message, cancellationToken);
                   return;
               }
               
               if (!int.TryParse(args[3], out var fat))
               {
                   await SendError("Wrong format", client, message, cancellationToken);
                   return;
               }
               
               if (!int.TryParse(args[4], out var carbohydrates))
               {
                   await SendError("Wrong format", client, message, cancellationToken);
                   return;
               }

               var portion = int.TryParse(args.Skip(5).FirstOrDefault(), out var p) ? p : 1;

               var foodInfo = new FoodInfo(kcal, protein, fat, carbohydrates);
               
               await kCalMeterService.WriteFood(name, portion, foodInfo, message.Chat.Id, message.From!.Id);
               await kCalMeterService.WritePortion(name, portion, message.Chat.Id, message.From!.Id, message.Date);
           }
           else
           {
               await client.SendTextMessageAsync(message.Chat.Id, "No command!", cancellationToken: cancellationToken);   
           }
        }
        catch (Exception e)
        {
            log.Error("BotDaemonException", e);
            Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            if (update.Message?.Chat.Id != null)
                await client.SendTextMessageAsync(update.Message.Chat.Id, "Exception!", cancellationToken: cancellationToken);
        }
    }

    private Task SendError(string text, ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        return client.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken);
    }

    private async Task ProcessCommand(Message message)
    {
        var messageText = message.Text!;

        if (messageText.StartsWith("/delete"))
        {
            var name = messageText.Replace("/delete", string.Empty).Trim();
            await kCalMeterService.DeleteFood(name, message.Chat.Id, message.From!.Id);
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