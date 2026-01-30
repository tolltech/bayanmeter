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
    PlannyJobRunner plannyJobRunner,
    IChatSettingsService chatSettingsService,
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
                replyMessageText = await CreateNewPlan(messageText, message);
                //await plannyJobRunner.Run();
            }
            else if (messageText.StartsWith("/all"))
            {
                replyMessageText = await GetAllPlans(message);
            }
            else if (messageText.StartsWith("/deletelast"))
            {
                replyMessageText = await DeleteLastPlan(message);
                //await plannyJobRunner.Run();
            }
            else if (messageText.StartsWith("/delete"))
            {
                replyMessageText = await DeletePlan(message, messageText);
                //await plannyJobRunner.Run();
            }
            else if (messageText.StartsWith("/chat"))
            {
                replyMessageText = await CreateChatSettings(message, messageText);
            }

            await client.SendMessage(message.Chat.Id, replyMessageText,
                cancellationToken: cancellationToken,
                replyParameters: new ReplyParameters { MessageId = message.MessageId });
        }
        catch (Exception e)
        {
            log.Error(e, "BotDaemonException");
            Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            if (update.Message?.Chat.Id != null)
                await client.SendMessage(update.Message.Chat.Id, "Exception!",
                    cancellationToken: cancellationToken);
        }
    }

    private static readonly HashSet<string> locales =
    [
        "en",
        "zh-Hans",
        "zh-Hant",
        "cz",
        "da",
        "nl",
        "fi",
        "fr",
        "de",
        "he",
        "hu",
        "it",
        "ja",
        "ko",
        "nb",
        "fa",
        "pl",
        "pt-BR",
        "ro",
        "ru",
        "sl-SI",
        "es",
        "es-MX",
        "sv",
        "vi",
        "tr",
        "uk",
        "el",
        "kk"
    ];

    private async Task<string> CreateChatSettings(Message message, string messageText)
    {
        var chatId = message.Chat.Id;
        var args = messageText.Replace("/chat", string.Empty).Trim();
        var splits = args.Split([" "], StringSplitOptions.RemoveEmptyEntries);
        var offset = int.Parse(splits.FirstOrDefault(x => int.TryParse(x, out _)) ?? "0");
        var locale = splits.FirstOrDefault(x => locales.Contains(x));
        var newChat = new ChatSettingsDbo
        {
            ChatId = chatId,
            Timestamp = DateTime.UtcNow.Ticks,
            Settings = new ChatSettings
            {
                Locale = locale ?? string.Empty,
                Offset = TimeSpan.FromHours(offset)
            }
        };

        await chatSettingsService.CreateOrUpdate(newChat);
        return $"Created chat settings with {locale} and {offset}";
    }

    private async Task<string> DeleteLastPlan(Message message)
    {
        var plan = await planService.DeleteLastByChat(message.Chat.Id);
        return $"Removed last plan {plan?.IntId} {plan?.Name}";
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
        var chatSettings = await chatSettingsService.Get(message.Chat.Id);
        var chatPlans = await planService.SelectByChatId(message.Chat.Id);
        return chatPlans
            .Select(x =>
                chatSettings?.Settings.Locale.ToLower() == "ru"
                ? $"{x.IntId} {x.Name} {CronExtensions.GetCronDescription(x.Cron, chatSettings.Settings.Locale, -chatSettings?.Settings.Offset)}. Следующий запуск {CronExtensions.NextRun(x.Cron, chatSettings?.Settings.Offset)}"
                : $"{x.IntId} {x.Name} {CronExtensions.GetCronDescription(x.Cron, chatSettings?.Settings.Locale ?? "en", -chatSettings?.Settings.Offset)}. next run {CronExtensions.NextRun(x.Cron, chatSettings?.Settings.Offset)}")
            .JoinToString(Environment.NewLine + Environment.NewLine);
    }

    private async Task<string> CreateNewPlan(string messageText, Message message)
    {
        var rawName = new string(messageText.SkipWhile(x => x != ' ').TakeWhile(x => x != ',').ToArray());
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Error: Empty name of plan";
        }

        var cronSource = new string(messageText.Replace(rawName, string.Empty).SkipWhile(x => x != ',').Skip(1)
            .TakeWhile(_ => true).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(cronSource))
        {
            return "Error: Empty cron of plan";
        }

        //(minute, hour, dayOfMonth, month, dayOfWeek)
        string? cron;
        CronExpression? cronExpression;
        if (CronExpression.TryParse(cronSource, out var expression))
        {
            cron = cronSource;
            cronExpression = expression;
        }
        else
        {
            cron = HumanToCronConverter.SafeConvertToCron(cronSource);
            if (cron == null || !CronExpression.TryParse(cron, out var expression2))
            {
                return "Error: Wrong cron format";
            }

            cronExpression = expression2;
        }

        var chatSettings = await chatSettingsService.Get(message.Chat.Id);

        var offset = chatSettings?.Settings.Offset ?? TimeSpan.Zero;
        var newCron = CronExtensions.TryApplyOffset(cron, offset, out var error);
        if (!string.IsNullOrWhiteSpace(error))
        {
            log.Error(error);
        }
        else
        {
            log.Info($"Apply {offset:g} offset to cron {cron} -> {newCron}");
            cron = newCron;
        }

        var newCronExpression = CronExpression.TryParse(newCron, out var exp) ? exp : cronExpression;
        var nextOccurrence = newCronExpression.GetNextOccurrence(DateTime.UtcNow) + offset;

        if (nextOccurrence == null)
        {
            return "Error:Cant get next occurrence of cron";
        }

        var splits = newCron.Split([" "], StringSplitOptions.RemoveEmptyEntries);
        if (splits.FirstOrDefault()?.Contains("*") ?? false)
        {
            return $"Too often cron {newCron}";
        }

        var descriptor = CronExtensions.GetCronDescription(cron, chatSettings?.Settings.Locale ?? "en", -offset);

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
            Cron = newCron,
            CronDescription = descriptor,
            CronSource = cronSource,
        };

        await planService.CreateOrUpdateByNameAndChat(plan);

        return chatSettings?.Settings.Locale.ToLower() == "ru" 
            ? $"Запланированно {rawName.Trim()} {descriptor}. Следующий запуск {nextOccurrence.Value:s}"
            : $"Create {rawName.Trim()} with {descriptor}. Next run is {nextOccurrence.Value:s}";
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