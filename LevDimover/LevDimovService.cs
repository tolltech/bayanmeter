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
            yield return GetPravReplaced(GetLevReplaced(word));
        }
    }

    private static string GetLevReplaced(string word)
    {
        if (new string(word.Where(char.IsLetter).ToArray()).ToLower().StartsWith("лев") || !word.Contains("лев"))
        {
            return word;
        }

        var leftSymbols = new string(word.TakeWhile(c => !char.IsLetterOrDigit(c)).ToArray());
        var realWord = new string(word.SkipWhile(c => !char.IsLetterOrDigit(c)).TakeWhile(char.IsLetterOrDigit)
            .ToArray());
        var rightSymbols = word.Replace($"{leftSymbols}{realWord}", string.Empty);

        return $"{leftSymbols}{realWord} {realWord.Replace("лев", "димов")}{rightSymbols}";
    }

    private static string GetPravReplaced(string word)
    {
        if (word.ToLower() == "прав") return "не лев";
        if (word.StartsWith("прав")) return word.Replace("прав", "не лев");
        return word.Replace("прав", "нелев");
    }
}