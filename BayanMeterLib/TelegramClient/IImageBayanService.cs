namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface IImageBayanService
    {
        int GetBayanMetric(byte[] imageBytes);
    }
}