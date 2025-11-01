using System.Text.RegularExpressions;

namespace Tolltech.Planny;

public static class HumanToCronConverter
{
    private static readonly Dictionary<string, string> MonthMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "января", "1" }, { "февраля", "2" }, { "марта", "3" }, { "апреля", "4" },
            { "мая", "5" }, { "июня", "6" }, { "июля", "7" }, { "августа", "8" },
            { "сентября", "9" }, { "октября", "10" }, { "ноября", "11" }, { "декабря", "12" },
            { "january", "1" }, { "february", "2" }, { "march", "3" }, { "april", "4" },
            { "may", "5" }, { "june", "6" }, { "july", "7" }, { "august", "8" },
            { "september", "9" }, { "october", "10" }, { "november", "11" }, { "december", "12" }
        };

    private static readonly Dictionary<string, string> DayOfWeekMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "понедельник", "1" }, { "вторник", "2" }, { "среда", "3" }, { "среду", "3" }, { "четверг", "4" },
            { "пятница", "5" }, { "пятницу", "5" }, { "суббота", "6" }, { "субботу", "6" }, { "воскресенье", "0" },
            { "monday", "1" }, { "tuesday", "2" }, { "wednesday", "3" }, { "thursday", "4" },
            { "friday", "5" }, { "saturday", "6" }, { "sunday", "0" }
        };

    public static string? SafeConvertToCron(string humanExpression)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(humanExpression))
                return null;

            humanExpression = humanExpression.ToLower().Trim();

            // Основные паттерны
            string? cron;
            if (TryParseWeekly(humanExpression, out cron)) return cron;
            if (TryParseMonthly(humanExpression, out cron)) return cron;
            if (TryParseEveryMinute(humanExpression, out cron)) return cron;
            if (TryParseDailyAtTime(humanExpression, out cron)) return cron;
            if (TryParseInterval(humanExpression, out cron)) return cron;
            if (TryParseSpecificDateTime(humanExpression, out cron)) return cron;

            return null;
        }
        catch (Exception e)
        {
            return null;
        }
    }

    private static bool TryParseEveryMinute(string expression, out string? cron)
    {
        cron = null;
        if (expression.Contains("каждую минуту") || expression.Contains("every minute"))
        {
            cron = "* * * * *";
            return true;
        }

        return false;
    }

    private static bool TryParseDailyAtTime(string expression, out string? cron)
    {
        cron = null;

        // Паттерн: "каждый день в 14:30"
        var match = Regex.Match(expression,
            @"(каждый день|every day|daily|ежедневно)\s+(в|at)?\s*(\d{1,2}):(\d{2})",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            match = Regex.Match(expression,
                @"в\s*(\d{1,2}):(\d{2})",
                RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var hour = match.Groups[match.Groups.Count - 2].Value;
            var minute = match.Groups[match.Groups.Count - 1].Value;
            cron = $"{minute} {hour} * * *";
            return true;
        }

        return false;
    }

    private static bool TryParseWeekly(string expression, out string? cron)
    {
        cron = null;

        // Паттерн: "каждый понедельник в 9:00"
        var match = Regex.Match(expression,
            @"(каждый|every|каждую|каждое)\s+(\w+)\s+(в|at)?\s*(\d{1,2}):(\d{2})",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var dayName = match.Groups[2].Value;
            var hour = match.Groups[4].Value;
            var minute = match.Groups[5].Value;

            if (DayOfWeekMap.TryGetValue(dayName, out var dayNumber))
            {
                cron = $"{minute} {hour} * * {dayNumber}";
                return true;
            }
        }

        return false;
    }

    private static bool TryParseMonthly(string expression, out string? cron)
    {
        cron = null;

        // Паттерн: "1-го числа каждого месяца в 6:00"
        var match = Regex.Match(expression,
            @"(\d+)(?:-го)?\s+числа\s+(каждого месяца|every month)\s+(в|at)?\s*(\d{1,2}):(\d{2})",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            match = Regex.Match(expression,
                @"(\d+)(?:st|nd|rd|th)?\s+(day of every month|of every month)\s+(at)?\s*(\d{1,2}):(\d{2})",
                RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var day = match.Groups[1].Value;
            var hourIndex = match.Groups.Count - 2;
            var minuteIndex = match.Groups.Count - 1;
            var hour = match.Groups[hourIndex].Value;
            var minute = match.Groups[minuteIndex].Value;

            cron = $"{minute} {hour} {day} * *";
            return true;
        }

        return false;
    }

    private static bool TryParseInterval(string expression, out string? cron)
    {
        cron = null;

        // Паттерн: "каждые 5 минут"
        var match = Regex.Match(expression,
            @"каждые\s+(\d+)\s+минут",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            match = Regex.Match(expression,
                @"every\s+(\d+)\s+minutes",
                RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var minutes = match.Groups[1].Value;
            cron = $"*/{minutes} * * * *";
            return true;
        }

        // Паттерн: "каждый час"
        if (expression.Contains("каждый час") || expression.Contains("every hour"))
        {
            cron = "0 * * * *";
            return true;
        }

        return false;
    }

    private static bool TryParseSpecificDateTime(string expression, out string? cron)
    {
        cron = null;

        // Паттерн: "в 23:59 31 декабря"
        var match = Regex.Match(expression,
            @"в\s*(\d{1,2}):(\d{2})\s+(\d{1,2})\s+(\w+)",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            match = Regex.Match(expression,
                @"at\s*(\d{1,2}):(\d{2})\s+(\d{1,2})\s+(\w+)",
                RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var hour = match.Groups[1].Value;
            var minute = match.Groups[2].Value;
            var day = match.Groups[3].Value;
            var monthName = match.Groups[4].Value;

            if (MonthMap.TryGetValue(monthName, out var month))
            {
                cron = $"{minute} {hour} {day} {month} *";
                return true;
            }
        }

        return false;
    }
}