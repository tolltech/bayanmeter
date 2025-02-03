using Tolltech.LevDimover;

namespace LevDimoverTest;

public class LevDimovServiceTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [TestCase("Хлев", ExpectedResult = "Хлев Хдимов")]
    [TestCase("первое слово Хлев второе", ExpectedResult = "первое слово Хлев Хдимов второе")]
    [TestCase("первое слово сказала корова второе слово сказал лев", ExpectedResult = "первое слово сказала корова второе слово сказал лев")]
    public string TestConvert(string input)
    {
        return LevDimovService.Convert(input);
    }
}