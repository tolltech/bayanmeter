namespace Tolltech.BayanMeterLib.Psql;

public static class MessageHelper
{
    public static string GetStrId(long chatId, int messageId) => $"{chatId}_{messageId}";
}