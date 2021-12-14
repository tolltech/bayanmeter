using JetBrains.Annotations;
using Tolltech.BayanMeterLib.Psql;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface IMemEasyService
    {
        [ItemNotNull] [NotNull] MessageDbo GetRandomMessages(long chatId);
    }
}