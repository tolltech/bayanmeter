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
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var jobData = context.JobDetail.JobDataMap;

            var planName = jobData.GetString(nameof(PlanDbo.Name));
            var chatId = jobData.GetLongValue(nameof(PlanDbo.ChatId));

            await telegramBotClient.SendTextMessageAsync(chatId, $"It's time to {planName}");
        }
        catch (Exception e)
        {
            log.Error(e, $"Error while doing planny job");
        }
    }
}

public class PlannyJobRunner(IPlanService planService, PlanJobFactory planJobFactory, ILog log)
{
    public async Task Run()
    {
        try
        {
            log.Info($"Getting plans for job");

            var plans = await planService.SelectAll();

            log.Info($"Got {plans.Length} plans");

            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            scheduler.JobFactory = planJobFactory;
            await scheduler.Start();

            foreach (var plan in plans)
            {
                try
                {
                    log.Info($"Scheduling {plan.Name} {plan.Id}");

                    var job = JobBuilder.Create<PlanJob>()
                        .WithIdentity(plan.Id.ToString(), plan.ChatId.ToString())
                        .UsingJobData(nameof(plan.ChatId), plan.ChatId)
                        .UsingJobData(nameof(plan.Name), plan.Name)
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
    }
}