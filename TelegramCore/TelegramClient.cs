using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Tolltech.CoreLib.Helpers;

namespace Tolltech.TelegramCore
{
    public class TelegramClient : ITelegramClient
    {
        private readonly TelegramBotClient client;

        public TelegramClient(TelegramBotClient client)
        {
            this.client = client;
        }

        public async Task<byte[]> GetFile(string fileId)
        {
            using (var stream = new MemoryStream())
            {
                var file = await client.GetFile(fileId);

                if (file.FilePath == null)
                {
                    throw new ArgumentException("File not found", nameof(fileId));
                }
                
                await client.DownloadFile(file.FilePath, stream);
                stream.Seek(0, SeekOrigin.Begin);

                return stream.ReadToByteArray();
            }
        }
    }
}