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
    [TestCase("первое слово левe второе корове", ExpectedResult = "первое слово левe второе корове")]
    [TestCase("первое слово сказала корова второе слово сказал лев", ExpectedResult = "первое слово сказала корова второе слово сказал лев")]
    [TestCase("ты слева? или как", ExpectedResult = "ты слева сдимова? или как")]
    [TestCase("ты ,слева или как", ExpectedResult = "ты ,слева сдимова или как")]
    [TestCase("ты   , слева или   как", ExpectedResult = "ты   , слева сдимова или   как")]
    [TestCase("ты ,,слева?? или как", ExpectedResult = "ты ,,слева сдимова?? или как")]
    public string TestConvert(string input)
    {
        return LevDimovService.Convert(input);
    }
}