using CronExpressionDescriptor;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.TelegramCore;
using Vostok.Logging.Abstractions;

namespace Tolltech.Planny;

public class PlannyBotDaemon(
    [FromKeyedServices(PlannyBotDaemon.Key)]TelegramBotClient telegramBotClient,
    IPlanService planService,
    ILog log) : IBotDaemon
{
    public const string Key = "Planny";
    

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

            if (!messageText.StartsWith("/"))
            {
                return;
            }

            var replyMessageText = string.Empty;
            if (messageText.StartsWith("/new"))
            {
                replyMessageText = CreateNewPlan(messageText, message);
            }

            await client.SendTextMessageAsync(message.Chat.Id, replyMessageText,
                cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId);
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

    private string CreateNewPlan(string messageText, Message message)
    {
        var rawName = new string(messageText.SkipWhile(x => x != ' ').TakeWhile(x => x != ',').ToArray());
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Error: Empty name of plan";
        }

        var cron = new string(messageText.Replace(rawName, string.Empty).SkipWhile(x => x != ',').Skip(1)
            .TakeWhile(_ => true).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(cron))
        {
            return "Error: Empty cron of plan";
        }

        //(minute, hour, dayOfMonth, month, dayOfWeek)

        if (!CronExpression.TryParse(cron, out var expression))
        {
            return "Error: Wrong format of cron";
        }

        var nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);

        if (nextUtc == null)
        {
            return "Error:Cant get next occurrence of cron";
        }

        var descriptor = ExpressionDescriptor.GetDescription(cron, new Options
        {
            DayOfWeekStartIndexZero = false,
            Use24HourTimeFormat = true
        });

        var plan = new PlanDbo
        {
            Id = Guid.NewGuid(),
            ChatId = message.Chat.Id,
            Name = rawName.Trim(),
            FromMessageId = message.MessageId,
            FromUserId = message.From?.Id ?? 0,
            CreateDate = DateTimeOffset.UtcNow,
            FromUserName = message.From?.Username ?? "Unknown",
            Timestamp = DateTime.UtcNow.Ticks,
            Cron = cron,
            CronDescription = descriptor
        };
        
        planService.CreateOrUpdateByNameAndChat(plan);

        return $"Create {rawName.Trim()} with {descriptor}. Next run is {nextUtc.Value:s}";
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