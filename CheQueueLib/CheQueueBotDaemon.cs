using System;
using System.Linq;
using log4net;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Tolltech.TelegramCore;

namespace Tolltech.CheQueueLib
{
    public class CheQueueBotDaemon : IBotDaemon
    {
        private readonly TelegramBotClient client;
        private readonly ITelegramClient telegramClient;
        private readonly ISettings settings;

        private static readonly ILog log = LogManager.GetLogger(typeof(CheQueueBotDaemon));

        public CheQueueBotDaemon(TelegramBotClient client, ITelegramClient telegramClient, ISettings settings)
        {
            this.client = client;
            this.telegramClient = telegramClient;
            this.settings = settings;
        }

        public void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;

                log.Info($"RecieveMessage {message.Chat.Id} {message.MessageId}");

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

                var fromChatId = message.Chat.Id;
                var sendChatId = long.TryParse(settings.SpecialForAnswersChatId, out var chatId)
                    ? chatId
                    : message.Chat.Id;

                if (fromChatId == sendChatId)
                {
                    client.SendTextMessageAsync(sendChatId, $"Get jpeg {bytes.Length} bytes", replyToMessageId: message.MessageId).GetAwaiter().GetResult();
                }
                else
                {
                    client.ForwardMessageAsync(sendChatId, fromChatId, message.MessageId).GetAwaiter().GetResult();
                    client.SendTextMessageAsync(sendChatId, $"Get jpeg {bytes.Length} bytes").GetAwaiter().GetResult();
                }
                
            }
            catch (Exception e)
            {
                log.Error("CheQueueBotDaemon", e);
                Console.WriteLine($"CheQueueBotDaemon: {e.Message} {e.StackTrace}");
            }
        }
    }
}