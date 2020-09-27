using JetBrains.Annotations;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface IImageBayanService
    {
        void SaveMessage([NotNull] [ItemNotNull] params MessageDto[] messages);
        [NotNull] BayanResultDto GetBayanMetric([NotNull] string messageStrId);
    }
}