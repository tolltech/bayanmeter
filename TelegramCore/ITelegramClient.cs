using System.Threading.Tasks;

namespace Tolltech.TelegramCore
{
    public interface ITelegramClient
    {
        Task<byte[]> GetFile(string fileId);
    }
}