using Tolltech.BayanMeterLib.Psql;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface IMemEasyService
    {
        MessageDbo GetRandomMessages(long chatId);
        MessageDbo GetRandomTopMessages(long chatId);
    }
}