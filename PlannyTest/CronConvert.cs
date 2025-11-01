using Quartz;
using Quartz.Impl;
using Tolltech.Planny;
using Vostok.Logging.Abstractions;
using CronExpression = Cronos.CronExpression;

namespace PlannyTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [TestCase("", null)]
    [TestCase("каждый день в 14:30", "30 14 * * *")]
    [TestCase("каждый понедельник в 9:00", "00 9 * * 1")]
    [TestCase("каждую субботу в 9:00", "00 9 * * 6")]
    [TestCase("каждое воскресенье в 11:30", "30 11 * * 0")]
    [TestCase("каждый вторник", null)]
    [TestCase("1-го числа каждого месяца в 6:00", "00 6 1 * *")]
    [TestCase("каждые 5 минут", "*/5 * * * *")]
    public void TestHumanConvert(string human, string? expected)
    {
        var actual = HumanToCronConverter.SafeConvertToCron(human);
        Assert.That(actual, Is.EqualTo(expected));

        if (expected != null)
        {
            var success = CronExpression.TryParse(actual, out var expression);
            Assert.That(success, Is.EqualTo(true));
            Assert.That(expression?.GetNextOccurrence(DateTime.UtcNow), Is.Not.Null);
        }
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
    public async Task TestConvert(string unixCron, string expected, bool expectedException = false,
        bool expectedParseError = false)
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