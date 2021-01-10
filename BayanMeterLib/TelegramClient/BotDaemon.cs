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
        private readonly ISettings settings;

        private static readonly ILog log = LogManager.GetLogger(typeof(BotDaemon));

        public BotDaemon(TelegramBotClient client, ITelegramClient telegramClient, IImageBayanService imageBayanService,
            ISettings settings)
        {
            this.client = client;
            this.telegramClient = telegramClient;
            this.imageBayanService = imageBayanService;
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

                var messageDto = Convert(message, bytes);
                imageBayanService.SaveMessage(messageDto);

                log.Info($"SavedMessage {message.Chat.Id} {message.MessageId}");

                var bayanMetric = imageBayanService.GetBayanMetric(messageDto.StrId);

                log.Info($"GetBayanMetrics {bayanMetric.AlreadyWasCount} {message.Chat.Id} {message.MessageId}");

                if (bayanMetric.AlreadyWasCount > 0)
                {
                    var fromChatId = message.Chat.Id;
                    var sendChatId = long.TryParse(settings.SpecialForAnswersChatId, out var chatId)
                        ? chatId
                        : message.Chat.Id;

                    if (fromChatId == sendChatId)
                    {
                        client.SendTextMessageAsync(sendChatId, GetBayanMessage(bayanMetric), replyToMessageId: messageDto.IntId).GetAwaiter().GetResult();
                    }
                    else
                    {
                        client.ForwardMessageAsync(sendChatId, fromChatId, messageDto.IntId).GetAwaiter().GetResult();
                        client.SendTextMessageAsync(sendChatId, GetBayanMessage(bayanMetric)).GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("BotDaemonException", e);
                Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            }
        }

        private static string GetBayanMessage(BayanResultDto bayanMetric)
        {
            //" -1001261621141"
            var chatIdStr = bayanMetric.PreviousChatId.ToString();
            if (chatIdStr.StartsWith("-100"))
            {
                chatIdStr = chatIdStr.Replace("-100", string.Empty);
            }

            var chatId = long.Parse(chatIdStr);

            return $"[:||[{bayanMetric.AlreadyWasCount}]||:] #bayan\r\n" +
                   $"https://t.me/c/{chatId}/{bayanMetric.PreviousMessageId}";
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