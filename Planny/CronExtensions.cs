using CronExpressionDescriptor;
using Cronos;

namespace Tolltech.Planny;

public static class CronExtensions
{
    public static DateTime? NextRun(string cron)
    {
        if (!CronExpression.TryParse(cron, out var expression))
        {
            return null;
        }

        var nextUtc = expression.GetNextOccurrence(DateTime.UtcNow);
        return nextUtc;
    }

    public static string GetCronDescription(string cron)
    {
        var descriptor = ExpressionDescriptor.GetDescription(cron, new Options
        {
            DayOfWeekStartIndexZero = false,
            Use24HourTimeFormat = true
        });

        return descriptor;
    }
}