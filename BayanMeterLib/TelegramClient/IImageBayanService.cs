using System.Threading.Tasks;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface IImageBayanService
    {
        void CreateMessage(MessageDto message);
        Task UpdateReactions(int messageId, long chatId, long fromUserId, string[] reactions);
    }
}