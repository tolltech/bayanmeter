using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.TelegramCore;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class EasyMemeBotDaemon : IBotDaemon
    {
        public const string Key = "EasyMeme";

        private readonly ITelegramClient telegramClient;
        private readonly IImageBayanService imageBayanService;
        private readonly IMemEasyService memEasyService;

        private static readonly ILog log = LogManager.GetLogger(typeof(EasyMemeBotDaemon));

        public EasyMemeBotDaemon([FromKeyedServices(Key)] ITelegramClient telegramClient,
            IImageBayanService imageBayanService, IMemEasyService memEasyService)
        {
            this.telegramClient = telegramClient;
            this.imageBayanService = imageBayanService;
            this.memEasyService = memEasyService;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient client, Update update,
            CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Type != UpdateType.Message)
                return;

            try
            {
                var message = update.Message;
                if (message == null)
                {
                    return;
                }

                log.Info($"RecieveMessage {message.Chat.Id} {message.MessageId}");

                await SaveMessageIfPhotoAsync(message).ConfigureAwait(false);

                if (message.Text?.StartsWith(@"/easymeme") ?? false)
                {
                    await SendEasyMemeAsync(client, message.Chat.Id).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                log.Error("BotDaemonException", e);
                Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            log.Error("BotDaemonException", exception);
            Console.WriteLine($"BotDaemonException: {ErrorMessage} {exception.StackTrace}");
            return Task.CompletedTask;
        }

        private Task SendEasyMemeAsync(ITelegramBotClient client, long chatId)
        {
            var randomMessage = memEasyService.GetRandomMessages(chatId);
            return client.SendTextMessageAsync(chatId, "take it easy", replyToMessageId: randomMessage.IntId);
        }

        private Task SaveMessageIfPhotoAsync(Message message)
        {
            if (message?.Type != MessageType.Photo)
            {
                return Task.CompletedTask;
            }

            var photoSize = message.Photo?.FirstOrDefault();

            if (photoSize == null)
            {
                return Task.CompletedTask;
            }

            var bytes = telegramClient.GetFile(photoSize.FileId);

            var messageDto = Convert(message, bytes);
            imageBayanService.CreateMessage(messageDto);

            log.Info($"SavedMessage {message.Chat.Id} {message.MessageId}");

            return Task.CompletedTask;
            //var bayanMetric = imageBayanService.GetBayanMetric(messageDto.StrId);

            //log.Info($"GetBayanMetrics {bayanMetric.AlreadyWasCount} {message.Chat.Id} {message.MessageId}");

            //if (bayanMetric.AlreadyWasCount > 0)
            //{
            //    var fromChatId = message.Chat.Id;
            //    var sendChatId = long.TryParse(settings.SpecialForAnswersChatId, out var chatId)
            //        ? chatId
            //        : message.Chat.Id;

            //    if (fromChatId == sendChatId)
            //    {
            //        await client.SendTextMessageAsync(sendChatId, GetBayanMessage(bayanMetric), replyToMessageId: messageDto.IntId).ConfigureAwait(false);
            //    }
            //    else
            //    {
            //        await client.ForwardMessageAsync(sendChatId, fromChatId, messageDto.IntId).ConfigureAwait(false);
            //        await client.SendTextMessageAsync(sendChatId, GetBayanMessage(bayanMetric)).ConfigureAwait(false);
            //    }
            //}
        }

        //private static string GetBayanMessage(BayanResultDto bayanMetric)
        //{
        //    //" -1001261621141"
        //    var chatIdStr = bayanMetric.PreviousChatId.ToString();
        //    if (chatIdStr.StartsWith("-100"))
        //    {
        //        chatIdStr = chatIdStr.Replace("-100", string.Empty);
        //    }

        //    var chatId = long.Parse(chatIdStr);

        //    return $"[:||[{bayanMetric.AlreadyWasCount}]||:] #bayan\r\n" +
        //           $"https://t.me/c/{chatId}/{bayanMetric.PreviousMessageId}";
        //}

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
                FromUserName = message.From?.Username,
                FromUserId = message.From?.Id ?? 0,
                ImageBytes = bytes,
                StrId = $"{message.Chat.Id}_{message.MessageId}",
                Text = message.Text,
                ForwardFromUserId = message.ForwardFrom?.Id,
                ForwardFromUserName = message.ForwardFrom?.Username,
                ForwardFromChatId = message.ForwardFromChat?.Id,
                ForwardFromChatName = message.ForwardFromChat?.Username,
                ForwardFromMessageId = message.ForwardFromMessageId ?? 0,
            };
        }
    }
}