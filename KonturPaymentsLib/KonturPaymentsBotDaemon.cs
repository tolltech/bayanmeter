﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.SqlEF;
using Tolltech.TelegramCore;

namespace Tolltech.KonturPaymentsLib
{
    public class KonturPaymentsBotDaemon : IBotDaemon
    {
        private readonly IQueryExecutorFactory queryExecutorFactory;
        private readonly TelegramBotClient telegramBotClient;
        private static readonly ILog log = LogManager.GetLogger(typeof(KonturPaymentsBotDaemon));

        private readonly System.Timers.Timer timer;
        private static readonly ConcurrentBag<long> chatIds = new ConcurrentBag<long>();

        public KonturPaymentsBotDaemon(IQueryExecutorFactory queryExecutorFactory, TelegramBotClient telegramBotClient)
        {
            this.queryExecutorFactory = queryExecutorFactory;
            this.telegramBotClient = telegramBotClient;
            timer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);

            timer.Elapsed += async ( sender, e ) => await OnTimedEvent().ConfigureAwait(false);
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private async Task OnTimedEvent()
        {
            if (DateTime.UtcNow.Hour != 14)
            {
                return;
            }

            foreach (var chatId in chatIds.Distinct())
            {
                await SendReportAsync(telegramBotClient, chatId, 1).ConfigureAwait(false);
            }
        }

        public Task HandleErrorAsync(ITelegramBotClient client, Exception exception,
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

        public async Task HandleUpdateAsync(ITelegramBotClient client, Update update,
            CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;

            try
            {
                var message = update.Message;
                if (message == null || message.ForwardDate.HasValue || message.ReplyToMessage != null ||
                    message.Text == null)
                {
                    return;
                }

                log.Info($"RecieveMessage {message.Chat.Id} {message.MessageId}");

                chatIds.Add(message.Chat.Id);

                await SaveMessageIfAlertAsync(message).ConfigureAwait(false);

                if (message.Text.StartsWith(@"/stats"))
                {
                    var dayCount = int.TryParse(message.Text.Replace(@"/stats", string.Empty), out var d) ? d : 1;
                    await SendReportAsync(client, message.Chat.Id, dayCount).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                log.Error("BotDaemonException", e);
                Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            }
        }

        private Task SendReportAsync(ITelegramBotClient client, long chatId, int dayCount)
        {
            using var queryExecutor = queryExecutorFactory.Create<MoiraAlertHandler, MoiraAlertDbo>();
            var alerts = queryExecutor.Execute(f => f.Select(DateTime.UtcNow.AddDays(-dayCount).Ticks, chatId));

            var message = "```" + string.Join("\r\n",
                              new[] { "Name;Status;Count;Url" }
                                  .Concat(
                                      alerts.GroupBy(x => (x.AlertId, x.AlertStatus))
                                          .Where(x => x.Key.AlertStatus.Trim().ToLower() != "ok")
                                          .OrderByDescending(x => x.Count())
                                          .Select(x =>
                                              $"{x.First().AlertName};{x.Key.AlertStatus};{x.Count()};[{x.Key.AlertId}](https://moira.skbkontur.ru/trigger/{x.Key.AlertId})"))) +
                          "```";

            return client.SendTextMessageAsync(chatId, message, ParseMode.Markdown);
        }

        private Task SaveMessageIfAlertAsync([NotNull] Message message)
        {
            var text = message.Text;

            if (text == null || !text.Contains(@"moira.skbkontur.ru/trigger"))
            {
                return Task.CompletedTask;
            }

            var alert = new MoiraAlertDbo
            {
                Text = text,
                ChatId = message.Chat.Id,
                IntId = message.MessageId,
                MessageDate = message.Date,
                Timestamp = DateTime.UtcNow.Ticks,
                StrId = $"{message.Chat.Id}_{message.MessageId}",
                AlertId = GetAlertId(text),
                AlertName = GetAlertName(text),
                AlertStatus = GetAlertStatus(text),
                AlertText = GetAlertText(text),
            };

            using var queryExecutor = queryExecutorFactory.Create<MoiraAlertHandler, MoiraAlertDbo>();
            queryExecutor.Execute(f => f.Create(alert));
            return Task.CompletedTask;
        }

        [CanBeNull]
        private string GetAlertId(string text)
        {
            return text.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.Trim().StartsWith(@"https://moira.skbkontur.ru/trigger/"))
                ?.Replace(@"https://moira.skbkontur.ru/trigger/", string.Empty);
        }

        [CanBeNull]
        private string GetAlertName(string text)
        {
            return new string(text.Trim().SkipWhile(c => c != ' ').Skip(1).TakeWhile(c => c != '[').ToArray()).Trim();
        }

        [CanBeNull]
        private string GetAlertStatus(string text)
        {
            return new string(text.Trim().SkipWhile(c => !char.IsLetterOrDigit(c)).TakeWhile(c => c != ' ').ToArray())
                .Trim();
        }

        [CanBeNull]
        private string GetAlertText(string text)
        {
            var lines = text.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2) return null;

            return string.Join(";", lines.Skip(1).Take(lines.Length - 2));
        }

        private Task SendEasyMemeAsync(ITelegramBotClient client, long chatId)
        {
            //var randomMessage = memEasyService.GetRandomMessages(chatId);
            //return client.SendTextMessageAsync(chatId, "take it easy", replyToMessageId: randomMessage.IntId);
            return Task.CompletedTask;
        }
    }
}