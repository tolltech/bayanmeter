using System;
using System.Linq;
using log4net;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class BotDaemon : IBotDaemon
    {
        private readonly TelegramBotClient client;
        private readonly ITelegramClient telegramClient;
        private readonly IImageBayanService imageBayanService;

        private static readonly ILog log = LogManager.GetLogger(typeof(BotDaemon));

        public BotDaemon(TelegramBotClient client, ITelegramClient telegramClient, IImageBayanService imageBayanService)
        {
            this.client = client;
            this.telegramClient = telegramClient;
            this.imageBayanService = imageBayanService;
        }

        public void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
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

                var messageDto = Convert(message, bytes);
                imageBayanService.SaveMessage(messageDto);

                var bayanMetric = imageBayanService.GetBayanMetric(bytes);

                client.SendTextMessageAsync(message.Chat.Id,
                        $"{message.Text} {photoSize.Height} {photoSize.Width} {bytes.Length}. Metric - {bayanMetric}")
                    .GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                log.Error("BotDaemonException", e);
            }
        }

        private static MessageDto Convert(Message message, byte[] bytes)
        {
            var now = DateTime.UtcNow;
            return new MessageDto
            {
                MessageDate = message.Date,
                EditDate = message.EditDate,
                IntId = message.MessageId,
                ChatId = message.Chat.Id,
                Timestamp = now.Ticks,
                CreateDate = now,
                FromUserName = message.From.Username,
                FromUserId = message.From.Id,
                ImageBytes = bytes,
                StrId = $"{message.Chat.Id}_{message.MessageId}",
                Text = message.Text,
                ForwardFromUserId = message.ForwardFrom?.Id,
                ForwardFromUserName = message.ForwardFrom?.Username,
                ForwardFromChatId = message.ForwardFromChat?.Id,
                ForwardFromChatName = message.ForwardFromChat?.Username,
                ForwardFromMessageId = message.ForwardFromMessageId,
            };
        }
    }
}