namespace Tolltech.Planny;

public static class CronConverter
{
    public static bool TryConvertUnixToQuartz(string unixCron, out string result)
    {
        var parts = unixCron.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5)
            throw new ArgumentException("Invalid Unix cron expression");

        var minute = parts[0];
        var hour = parts[1];
        var dayOfMonth = parts[2];
        var month = parts[3];
        var dayOfWeek = parts[4];


        // Convert day of week (Unix: 0-6 where 0=Sunday, Quartz: 1-7 where 1=Sunday)
        var quartzDayOfWeek = ConvertDayOfWeek(dayOfWeek);

        result = unixCron;
        if (quartzDayOfWeek != "*" && dayOfMonth != "*")
        {
            return false;
        }

        if (quartzDayOfWeek == "*" && dayOfMonth == "*")
        {
            quartzDayOfWeek = "?";
        }
        else
        {
            if (quartzDayOfWeek == "*")
            {
                quartzDayOfWeek = "?";
            }

            if (dayOfMonth == "*")
            {
                dayOfMonth = "?";
            }
        }

        result = $"0 {minute} {hour} {dayOfMonth} {month} {quartzDayOfWeek}";
        return true;
    }

    private static string ConvertDayOfWeek(string unixDayOfWeek)
    {
        var map = new Dictionary<string, string>
        {
            { "0", "SUN" },
            { "1", "MON" },
            { "2", "TUE" },
            { "3", "WED" },
            { "4", "THU" },
            { "5", "FRI" },
            { "6", "SAT" },
            { "7", "SUN" },
        };

        var result = unixDayOfWeek;
        foreach (var (key, val) in map)
        {
            result = result.Replace(key, val);
        }

        return result;
    }
}