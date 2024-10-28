using System.Text;
using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.CoreLib.Helpers;
using Tolltech.SqlEF;
using Tolltech.TelegramCore;

namespace Tolltech.KCalMeter;

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
                await ProcessCommand(message, client, cancellationToken);
                return;
            }

            var args = message.Text?
                           .ToLower()
                           .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(x => x.Trim())
                           .ToArray()
                       ?? Array.Empty<string>();

            if (args.Length <= 2 && args.Length >= 1)
            {
                var name = args[0];
                var portion = 1m;
                if (args.Length > 1 && !decimal.TryParse(args[1], out portion))
                {
                    await SendError("Wrong format", client, message, cancellationToken);
                    return;
                }

                await kCalMeterService.WritePortion(name, portion, message.Chat.Id, message.From!.Id, message.Date);
                
                await client.SendTextMessageAsync(message.Chat.Id, "Portion done!", cancellationToken: cancellationToken,
                    replyToMessageId: message.MessageId);
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

                await client.SendTextMessageAsync(message.Chat.Id, "New food done!", cancellationToken: cancellationToken,
                    replyToMessageId: message.MessageId);
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
                await client.SendTextMessageAsync(update.Message.Chat.Id, "Exception!",
                    cancellationToken: cancellationToken);
        }
    }

    private Task SendError(string text, ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        return client.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken);
    }

    private async Task ProcessCommand(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var messageText = message.Text!;

        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        if (messageText.StartsWith("/delete"))
        {
            var name = messageText.GetArguments().First();
            await kCalMeterService.DeleteFood(name, chatId, userId);
        }
        else if (messageText.StartsWith("/last"))
        {
            if (!int.TryParse(messageText.GetArguments().FirstOrDefault(), out var count))
            {
                count = 10;
            }

            var portions = await kCalMeterService.SelectPortions(count, chatId, userId);

            var text = BuildReport(portions);

            await client.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId);
        }
        else if (messageText.StartsWith("/list"))
        {
            if (!int.TryParse(messageText.GetArguments().FirstOrDefault(), out var count))
            {
                count = 100;
            }

            var foods = await kCalMeterService.SelectFood(count, chatId, userId);

            var text = BuildReport(foods);

            await client.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId);
        }
        else if (messageText.StartsWith("/today"))
        {
            var portions = await kCalMeterService.SelectPortions(DateTime.Now, chatId, userId);

            var text = BuildReport(portions);

            await client.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId);
        }

        else if (messageText.StartsWith("/week"))
        {
            var portions = await kCalMeterService.SelectPortions(DateTime.Now.AddDays(-7), chatId, userId);

            var text = BuildReport(portions);

            await client.SendTextMessageAsync(message.Chat.Id, text, cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId);
        }
    }

    private string BuildReport(FoodDbo[] foods)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Join("\r\n",
            foods.OrderBy(x => x.Name)
                .Select(x => $"{x.Name} {x.Kcal} {x.Protein} {x.Fat} {x.Carbohydrate}")));
        
        return text.ToString();
    }

    private static string BuildReport(FoodMessageDbo[] portions)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Join("\r\n",
            portions.OrderBy(x => x.MessageDate)
                .Select(x => $"{x.Name} {x.Kcal} {x.Protein} {x.Fat} {x.Carbohydrate}")));

        text.AppendLine();
        text.AppendLine(
            $"Total {portions.Sum(x => x.Kcal)} {portions.Sum(x => x.Protein)} {portions.Sum(x => x.Fat)} {portions.Sum(x => x.Carbohydrate)}");
        return text.ToString();
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