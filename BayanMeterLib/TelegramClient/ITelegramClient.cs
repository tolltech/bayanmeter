namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface ITelegramClient
    {
        byte[] GetPhoto(string fileId);
    }
}