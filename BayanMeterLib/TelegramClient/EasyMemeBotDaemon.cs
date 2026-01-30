using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.BayanMeterLib.Psql;
using Tolltech.CoreLib.Helpers;
using Tolltech.TelegramCore;
using Vostok.Logging.Abstractions;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class EasyMemeBotDaemon(
        [FromKeyedServices(EasyMemeBotDaemon.Key)] ITelegramClient telegramClient,
        IImageBayanService imageBayanService,
        IMemEasyService memEasyService,
        ILog log)
        : IBotDaemon
    {
        public const string Key = "EasyMeme";

        private readonly ITelegramClient telegramClient = telegramClient;

        public async Task HandleUpdateAsync(ITelegramBotClient client, Update update,
            CancellationToken cancellationToken)
        {
            try
            {
                log.Info(
                    $"Received update chat {update.Message?.Chat?.Id} msg {update.Message?.MessageId} type {update.Type}");
                
                if (update.Message?.Chat.Id == -1001462479991)
                {
                    var text = $"Message {JsonConvert.SerializeObject(update, Formatting.Indented)}";
                    log.Info(text);
                    await client.SendMessage(update.Message!.Chat.Id,
                        text, cancellationToken: cancellationToken);
                }
                
                if (update.Type == UpdateType.MessageReaction && update.MessageReaction != null)
                {
                    await ProcessMessageReaction(update.MessageReaction, client);
                }

                // Only process Message updates: https://core.telegram.org/bots/api#message
                if (update.Type != UpdateType.Message)
                    return;
                
                var message = update.Message;
                if (message == null)
                {
                    return;
                }

                log.Info($"ReceiveMessage {message.Chat.Id} {message.MessageId}");

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

        private async Task ProcessMessageReaction(MessageReactionUpdated updateMessageReaction,
            ITelegramBotClient client)
        {
            var newReactions = updateMessageReaction.NewReaction;
            var messageId = updateMessageReaction.MessageId;
            var chatId = updateMessageReaction.Chat.Id;
            var fromUserId = updateMessageReaction.User?.Id;

            if (fromUserId == null) return;

            var reactions = new HashSet<string>(newReactions.Length);
            foreach (var reaction in newReactions)
            {
                var text = reaction switch
                {
                    ReactionTypeCustomEmoji reactionTypeCustomEmoji => reactionTypeCustomEmoji.CustomEmojiId,
                    ReactionTypeEmoji reactionTypeEmoji => reactionTypeEmoji.Emoji,
                    ReactionTypePaid reactionTypePaid => null,
                    _ => throw new ArgumentOutOfRangeException(nameof(reaction))
                };

                if (text == null) continue;
                
                reactions.Add(text);
            }

            if (chatId == -1001462479991)
            {
                await client.SendMessage(chatId, $"reactions {reactions.JoinToString(" ")}",
                    replyParameters: new ReplyParameters { MessageId = messageId });
            }

            await imageBayanService.UpdateReactions(messageId, chatId, fromUserId.Value, reactions.ToArray());
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
            return client.SendMessage(chatId, "take it easy",
                replyParameters: new ReplyParameters { MessageId = randomMessage.IntId });
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

            var messageDto = Convert(message);
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

        private static MessageDto Convert(Message message)
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
                StrId = MessageHelper.GetStrId(message.Chat.Id, message.MessageId),
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