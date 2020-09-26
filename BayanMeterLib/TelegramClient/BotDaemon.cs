using System.IO;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class BotDaemon : IBotDaemon
    {
        private readonly TelegramBotClient client;
        private readonly ITelegramClient telegramClient;

        public BotDaemon(TelegramBotClient client, ITelegramClient telegramClient)
        {
            this.client = client;
            this.telegramClient = telegramClient;
        }

        public void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message?.Type != MessageType.Photo)
            {
                return;
            }

            var photoSize = message?.Photo?.FirstOrDefault();

            if (photoSize == null)
            {
                return;
            }

            var bytes = telegramClient.GetPhoto(photoSize.FileId);
            
            File.WriteAllBytes("test.jpg", bytes);

            // if (string.IsNullOrEmpty(photo))
            // {
            //     return;
            // }

            client.SendTextMessageAsync(message.Chat.Id, $"{message.Text} {photoSize.Height} {photoSize.Width} {bytes.Length}").GetAwaiter().GetResult();
        }
    }
}