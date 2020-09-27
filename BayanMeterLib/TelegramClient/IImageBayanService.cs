using JetBrains.Annotations;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface IImageBayanService
    {
        void SaveMessage([NotNull] [ItemNotNull] params MessageDto[] messages);
        int GetBayanMetric([NotNull] byte[] imageBytes);
    }
}