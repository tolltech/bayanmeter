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
            if (new string(word.Where(char.IsLetter).ToArray()).ToLower().StartsWith("лев"))
            {
                yield return word;
            }
            else if (!word.Contains("лев"))
            {
                yield return word;
            }
            else
            {
                var leftSymbols = new string(word.TakeWhile(c => !char.IsLetterOrDigit(c)).ToArray());
                var realWord = new string(word.SkipWhile(c => !char.IsLetterOrDigit(c)).TakeWhile(char.IsLetterOrDigit)
                    .ToArray());
                var rightSymbols = word.Replace($"{leftSymbols}{realWord}", string.Empty);

                yield return $"{leftSymbols}{realWord} {realWord.Replace("лев", "димов")}{rightSymbols}";
            }
        }
    }
}