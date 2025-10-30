using log4net;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Telegram.Bot;

namespace Tolltech.Planny;

public class PlanJobFactory(TelegramBotClient telegramBotClient) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return new PlanJob(telegramBotClient);
    }

    public void ReturnJob(IJob job)
    {
    }
}

public class PlanJob(TelegramBotClient telegramBotClient) : IJob
{
    private static readonly ILog log = LogManager.GetLogger(typeof(PlanJob));
    
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
            log.Error($"Error while doing planny job", e);
        }
    }
}

public class PlannyJobRunner(IPlanService planService, PlanJobFactory planJobFactory)
{
    private static readonly ILog log = LogManager.GetLogger(typeof(PlannyBotDaemon));

    public async Task Run()
    {
        try
        {
            var plans = await planService.SelectAll();

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

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity(plan.Id.ToString(), plan.ChatId.ToString())
                        .WithCronSchedule(plan.Cron)
                        .Build();

                    
                    log.Info($"Scheduled {plan.Name} {plan.Id}");
                    await scheduler.ScheduleJob(job, trigger);
                }
                catch (Exception e)
                {
                    log.Error($"Fail to schedule {plan.Name} {plan.Id}", e);
                }
            }
        }
        catch (Exception e)
        {
            log.Error($"Scheduler didn't start", e);
        }
    }
}