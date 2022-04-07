using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Telegram.Bot;
using Telegram.Bot.Types;
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

        public Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        public async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type != UpdateType.Message)
                {
                    return;
                }

                var message = update.Message;

                log.Info($"RecieveMessage {message?.Chat.Id} {message?.MessageId}");

                if (message?.Type != MessageType.Photo)
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
                    var bytes = telegramClient.GetFile(photoSize.FileId);

                    var text = imageParser.Parse(bytes);

                    var fromChatId = message.Chat.Id;
                    var sendChatId = long.TryParse(settings.SpecialForAnswersChatId, out var chatId)
                        ? chatId
                        : message.Chat.Id;

                    log.Info($"CheQueueBotDaemon answer to chat {sendChatId}");

                    if (fromChatId == sendChatId)
                    {
                        await client.SendTextMessageAsync(sendChatId, $"Get jpeg {bytes.Length} bytes and \r\n{text}", replyToMessageId: message.MessageId, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await client.ForwardMessageAsync(sendChatId, fromChatId, message.MessageId, cancellationToken: cancellationToken).ConfigureAwait(false);
                        await client.SendTextMessageAsync(sendChatId, $"Get jpeg {bytes.Length} bytes and \r\n{text}", cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("CheQueueBotDaemon: " , e);
            }

        }
    }
}