using CronExpressionDescriptor;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.CoreLib.Helpers;
using Tolltech.TelegramCore;
using Vostok.Logging.Abstractions;

namespace Tolltech.Planny;

public class PlannyBotDaemon(
    [FromKeyedServices(PlannyBotDaemon.Key)]
    TelegramBotClient telegramBotClient,
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
            else if (messageText.StartsWith("/all"))
            {
                replyMessageText = await GetAllPlans(message);
            }
            else if (messageText.StartsWith("/delete"))
            {
                replyMessageText = await DeletePlan(message, messageText);
            }

            await client.SendTextMessageAsync(message.Chat.Id, replyMessageText,
                cancellationToken: cancellationToken,
                replyToMessageId: message.MessageId);
        }
        catch (Exception e)
        {
            log.Error(e, "BotDaemonException");
            Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            if (update.Message?.Chat.Id != null)
                await client.SendTextMessageAsync(update.Message.Chat.Id, "Exception!",
                    cancellationToken: cancellationToken);
        }
    }

    private async Task<string> DeletePlan(Message message, string messageText)
    {
        var arg = new string(messageText.SkipWhile(x => x != ' ').ToArray()).Trim();
        if (!int.TryParse(arg, out var intId) && string.IsNullOrWhiteSpace(arg))
        {
            return "Wrong plan number";
        }

        var plan = await planService.DeleteByIdOrChatAndName(intId, message.Chat.Id, arg);
        return $"Removed plan {plan?.IntId} {plan?.Name}";
    }

    private async Task<string> GetAllPlans(Message message)
    {
        var chatPlans = await planService.SelectByChatId(message.Chat.Id);
        return chatPlans
            .Select(x => $"{x.IntId} {x.Name}")
            .JoinToString(Environment.NewLine);
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

        log.Error(exception, "BotDaemonException");
        Console.WriteLine($"BotDaemonException: {errorMessage} {exception.StackTrace}");
        return Task.CompletedTask;
    }
}