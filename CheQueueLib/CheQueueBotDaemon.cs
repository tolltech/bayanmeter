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
        private readonly IImageParser imageParser;

        private static readonly ILog log = LogManager.GetLogger(typeof(CheQueueBotDaemon));

        public CheQueueBotDaemon(TelegramBotClient client, ITelegramClient telegramClient, ISettings settings,
            IImageParser imageParser)
        {
            this.client = client;
            this.telegramClient = telegramClient;
            this.settings = settings;
            this.imageParser = imageParser;
        }

        public void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;

                log.Info($"RecieveMessage {message.Chat.Id} {message.MessageId}");

                if (message.Type != MessageType.Photo)
                {
                    return;
                }

                var photoSizes = message.Photo;

                if (photoSizes == null || photoSizes.Length == 0)
                {
                    return;
                }

                foreach (var photoSize in photoSizes.OrderByDescending(x => x.Height * x.Width).Take(1))
                {
                    var bytes = telegramClient.GetPhoto(photoSize.FileId);

                    var text = imageParser.Parse(bytes);

                    var fromChatId = message.Chat.Id;
                    var sendChatId = long.TryParse(settings.SpecialForAnswersChatId, out var chatId)
                        ? chatId
                        : message.Chat.Id;

                    log.Info($"CheQueueBotDaemon answer to chat {sendChatId}");

                    if (fromChatId == sendChatId)
                    {
                        client.SendTextMessageAsync(sendChatId, $"Get jpeg {bytes.Length} bytes and \r\n{text}",
                            replyToMessageId: message.MessageId).GetAwaiter().GetResult();
                    }
                    else
                    {
                        client.ForwardMessageAsync(sendChatId, fromChatId, message.MessageId).GetAwaiter().GetResult();
                        client.SendTextMessageAsync(sendChatId, $"Get jpeg {bytes.Length} bytes and \r\n{text}")
                            .GetAwaiter().GetResult();
                    }
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