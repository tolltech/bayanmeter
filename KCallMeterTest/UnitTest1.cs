using Tolltech.KCalMeter;

namespace KCallMeterTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [TestCase("можжевеловый латте 1 2 3 4","можжевеловый латте;1;2;3;4")]
    [TestCase("можжевеловый латте макиато 1","можжевеловый латте макиато;1")]
    [TestCase("можжевеловый латте макиато","можжевеловый латте макиато")]
    public void Test1(string text, string expected)
    {
        var actual = KCalMeterBotDaemon.GetArgsFromMessage(text);
        Assert.That(expected, Is.EqualTo(string.Join(";", actual)));
    }
}