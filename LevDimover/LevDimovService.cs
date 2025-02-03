namespace Tolltech.LevDimover;

public static class LevDimovService
{
    public static string Convert(string message)
    {
        var words = message.Split(' ');
        var newWords = GetNewWords(words).ToArray();
        return string.Join(" ", newWords);
    }

    private static IEnumerable<string> GetNewWords(IEnumerable<string> words)
    {
        foreach (var word in words)
        {
            if (new string(word.Where(char.IsLetter).ToArray()).ToLower() == "лев")
            {
                yield return word;
            }
            else if (!word.Contains("лев"))
            {
                yield return word;
            }
            else
            {
                yield return $"{word} {word.Replace("лев", "димов")}";   
            }
        }
    }
}