using Tolltech.Planny;

namespace PlannyTest;

public class CronOffset
{
    [Test]
    [TestCaseSource(nameof(Cases))]
    public void ApplyOffset(string cron, TimeSpan offset, string expected)
    {
        var actual = CronExtensions.TryApplyOffset(cron, offset, out _);
        Assert.That(actual, Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> Cases()
    {
        yield return new TestCaseData("0 4 * * *", TimeSpan.FromHours(4), "0 0 * * *");
        yield return new TestCaseData("0 6 * * *", TimeSpan.FromHours(4), "0 2 * * *");
        yield return new TestCaseData("12 6 * * *", TimeSpan.FromHours(4), "12 2 * * *");
        yield return new TestCaseData("0 3 * * *", TimeSpan.FromHours(4), "0 3 * * *");
        yield return new TestCaseData("0 23 * * *", TimeSpan.FromHours(25), "0 23 * * *");
    }
}