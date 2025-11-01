using CronExpressionDescriptor;
using Cronos;
using Tolltech.CoreLib.Helpers;

namespace Tolltech.Planny;

public static class CronExtensions
{
    public static DateTime? NextRun(string cron, TimeSpan? offset = null)
    {
        var realOffset = offset ?? TimeSpan.Zero;
        if (!CronExpression.TryParse(cron, out var expression))
        {
            return null;
        }

        var nextUtc = expression.GetNextOccurrence(DateTime.UtcNow) + realOffset;
        return nextUtc;
    }

    public static string GetCronDescription(string cron, string locale = "en")
    {
        var descriptor = ExpressionDescriptor.GetDescription(cron, new Options
        {
            //DayOfWeekStartIndexZero = false,
            Use24HourTimeFormat = true,
            Locale = locale
        });

        return descriptor;
    }
    
    public static string TryApplyOffset(string cron, TimeSpan offset, out string errorLog)
    {
        errorLog = string.Empty;
        if (offset == TimeSpan.Zero) return cron;
        
        var hours =  offset.TotalHours;
        if (hours >= 24)
        {
            errorLog = $"Wrong offset {offset:g}";
            return cron;
        }

        var cronSplits = cron.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        if (cronSplits.Length < 2) return cron;
        var cronHourStr = cronSplits[1];
        if (!int.TryParse(cronHourStr, out var cronHour))
        {
            return cron;
        }

        var delta = cronHour - offset.Hours;
        if (delta is < 0 or > 23)
        {
            errorLog = $"Too hard to apply offset to cron {cron} {offset:g}";
            return cron;
        }

        cronSplits[1] = delta.ToString();
        var newCron = cronSplits.JoinToString(" ");
        
        var newNextOccurence = CronExtensions.NextRun(newCron, offset);
        if (newNextOccurence == null)
        {
            errorLog = $"Wrong new cron {cron} {offset:g} -> {newCron}";
            return cron;
        }

        var oldNextOccurence = CronExtensions.NextRun(cron);
        if (newNextOccurence != oldNextOccurence)
        {
            errorLog = $"Wrong nextOccurrences {oldNextOccurence:s} {newNextOccurence:s} {cron} {offset:g} -> {newCron}";
            return cron;
        }
        
        return newCron;
    }

}