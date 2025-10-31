using Quartz;
using Quartz.Impl;
using Tolltech.Planny;
using Vostok.Logging.Abstractions;

namespace PlannyTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [TestCase("0 * * * *", "0 0 * * * ?")]
    [TestCase("*/5 * * * *", "0 */5 * * * ?")]
    [TestCase("0 0 * * *", "0 0 0 * * ?")]
    [TestCase("0 2 * * *", "0 0 2 * * ?")]
    [TestCase("0 9 * * 1", "0 0 9 ? * MON")]
    [TestCase("0 0 1 * *", "0 0 0 1 * ?")]
    [TestCase("0 12 * * 0", "0 0 12 ? * SUN")]
    [TestCase("30 18 * * 1-5", "0 30 18 ? * MON-FRI")]
    [TestCase("0 * 123 * *", "0 0 * 123 * ?", true)]
    [TestCase("0 * 1 * 1", "0 * 1 * 1", true, true)]
    public async Task TestConvert(string unixCron, string expected, bool expectedException = false, bool expectedParseError = false)
    {
        var success = CronConverter.TryConvertUnixToQuartz(unixCron, out var actual);
        Assert.That(success, Is.EqualTo(!expectedParseError));
        Assert.That(actual, Is.EqualTo(expected));

        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        scheduler.JobFactory = new PlanJobFactory(null!, new SilentLog());

        if (!expectedException)
        {
            TriggerBuilder.Create()
                .WithIdentity(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
                .WithCronSchedule(actual).Build();
        }
        else
        {
            Assert.Throws<FormatException>(() =>
            {
                TriggerBuilder.Create()
                    .WithIdentity(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
                    .WithCronSchedule(actual).Build();
            });
        }
    }
}