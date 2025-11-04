using System.Collections.Concurrent;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Telegram.Bot;
using Vostok.Logging.Abstractions;

namespace Tolltech.Planny;

public class PlanJobFactory(TelegramBotClient telegramBotClient, ILog log) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return new PlanJob(telegramBotClient, log);
    }

    public void ReturnJob(IJob job)
    {
    }
}

public class PlanJob(TelegramBotClient telegramBotClient, ILog log) : IJob
{
    internal const string MsgTextName = "MsgText";
    
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var jobData = context.JobDetail.JobDataMap;

            var msgText = jobData.GetString(MsgTextName);
            var chatId = jobData.GetLongValue(nameof(PlanDbo.ChatId));

            await telegramBotClient.SendTextMessageAsync(chatId, msgText ?? "Unknown");
        }
        catch (Exception e)
        {
            log.Error(e, $"Error while doing planny job");
        }
    }
}

public class PlannyJobRunner(IPlanService planService, PlanJobFactory planJobFactory, IChatSettingsService chatSettingsService, ILog log)
{
    private static readonly ConcurrentDictionary<Guid, PlanDbo> runPlanIds = new();
    private static IScheduler? scheduler;
    private static readonly object locker = new();

    public async Task<int> Run()
    {
        var result = 0;
        try
        {
            log.Info($"Getting plans for job");

            var plans = await planService.SelectAll();

            log.Info($"Got {plans.Length} plans");

            var schedulerFactory = new StdSchedulerFactory();
            if (scheduler == null)
            {
                var newScheduler = await schedulerFactory.GetScheduler();
                newScheduler.JobFactory = planJobFactory;
                await newScheduler.Start();
                if (scheduler == null)
                {
                    lock (locker)
                    {
                        scheduler ??= newScheduler;
                    }
                }
            }

            var actualPlanIds = plans.Select(x => x.Id).ToHashSet();
            var scheduledPlanIds = runPlanIds.Select(x => x.Key).ToArray();
            foreach (var planId in scheduledPlanIds)
            {
                if (actualPlanIds.Contains(planId)) continue;

                runPlanIds.TryRemove(planId, out _);
                await scheduler.DeleteJob(new JobKey(planId.ToString()));
            }

            foreach (var plan in plans)
            {
                try
                {
                    if (runPlanIds.ContainsKey(plan.Id))
                    {
                        continue;
                    }

                    log.Info($"Scheduling {plan.Name} {plan.Id}");
                    
                    var chatSettings = await chatSettingsService.Get(plan.ChatId);

                    var message = chatSettings?.Settings.Locale.ToLower() == "ru"
                        ? $"Самое время {plan.Name}"
                        : $"It's time to {plan.Name}";
                    
                    var job = JobBuilder.Create<PlanJob>()
                        .WithIdentity(plan.Id.ToString())
                        .UsingJobData(nameof(plan.ChatId), plan.ChatId)
                        .UsingJobData(PlanJob.MsgTextName, message)
                        .Build();

                    if (!CronConverter.TryConvertUnixToQuartz(plan.Cron, out var quartzCron))
                    {
                        log.Error($"Can't convert cron to quartz {plan.Cron} {plan.Name} {plan.Id}");
                    }

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(plan.Id.ToString(), plan.ChatId.ToString())
                        .WithCronSchedule(quartzCron)
                        .Build();

                    log.Info($"Scheduled {plan.Name} {plan.Id}");
                    await scheduler.ScheduleJob(job, trigger);
                    result++;
                    runPlanIds[plan.Id] = plan;

                    if (!Cronos.CronExpression.TryParse(plan.Cron, out var expression))
                    {
                        log.Error($"Error: Wrong format of cron {plan.Cron} {plan.Name} {plan.Id}");
                    }
                    else
                    {
                        var nextQuartz = trigger.GetNextFireTimeUtc();
                        var nextUnix = expression.GetNextOccurrence(DateTime.UtcNow);

                        if (nextQuartz?.Date != nextUnix?.Date
                            || nextQuartz?.Hour != nextUnix?.Hour
                            || nextQuartz?.Minute != nextUnix?.Minute)
                        {
                            log.Error($"Different trigger time cron {nextUnix:s} quartz {nextQuartz:s}");
                        }
                        else
                        {
                            log.Info($"Next occurrences are the same {plan.Cron} {plan.Name} {plan.Id}");
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error(e, $"Fail to schedule {plan.Name} {plan.Id}");
                }
            }
        }
        catch (Exception e)
        {
            log.Error(e, $"Scheduler didn't start");
        }

        return result;
    }
}