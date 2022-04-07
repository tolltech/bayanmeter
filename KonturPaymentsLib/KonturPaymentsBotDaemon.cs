using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private readonly ITelegramClient telegramClient;
        private static readonly ILog log = LogManager.GetLogger(typeof(KonturPaymentsBotDaemon));

        private static System.Timers.Timer timer;
        private static readonly HashSet<long> chatIds = new HashSet<long>();

        private static readonly ConcurrentDictionary<DateTime, int> timerDates =
            new ConcurrentDictionary<DateTime, int>();

        public KonturPaymentsBotDaemon(IQueryExecutorFactory queryExecutorFactory, TelegramBotClient telegramBotClient,
            ITelegramClient telegramClient)
        {
            this.queryExecutorFactory = queryExecutorFactory;
            this.telegramBotClient = telegramBotClient;
            this.telegramClient = telegramClient;
        }

        private void OnTimedEvent()
        {
            var utcNow = DateTime.UtcNow;
            //Console.WriteLine($"BotDaemon: Timer {utcNow} chatIds {string.Join(",", chatIds.Distinct())}");

            if (timerDates.ContainsKey(utcNow.Date))
            {
                return;
            }

            if (utcNow.Hour != 14)
            {
                return;
            }

            foreach (var chatId in chatIds.Distinct())
            {
                SendReportAsync(telegramBotClient, chatId, 1).GetAwaiter().GetResult();
            }

            timerDates.AddOrUpdate(utcNow.Date, time => 1, (time, i) => i + 1);
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
                if (message == null || message.ForwardDate.HasValue || message.ReplyToMessage != null)
                {
                    return;
                }

                log.Info($"RecieveMessage {message.Chat.Id} {message.MessageId}");

                if (message.Text?.StartsWith(@"/stats") ?? false)
                {
                    var dayCount = int.TryParse(message.Text.Replace(@"/stats", string.Empty), out var d) ? d : 1;
                    await SendReportAsync(client, message.Chat.Id, dayCount).ConfigureAwait(false);
                    return;
                }

                var documment = message.Document;

                if (documment == null || string.IsNullOrWhiteSpace(documment.FileId))
                {
                    return;
                }

                var file = telegramClient.GetFile(documment.FileId);

                var chatHistory = JsonConvert.DeserializeObject<ChatDto>(Encoding.UTF8.GetString(file));

                chatIds.Add(message.Chat.Id);

                if (timer == null)
                {
                    timer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);

                    timer.Elapsed += (sender, e) => OnTimedEvent();
                    timer.AutoReset = true;
                    timer.Enabled = true;
                }

                await SaveMessageIfAlertAsync(chatHistory, message.Chat.Id).ConfigureAwait(false);
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

            var message = string.Join("\r\n",
                new[] { "Name;Status;Count;Url" }
                    .Concat(
                        alerts.GroupBy(x => (x.AlertId, x.AlertStatus))
                            .Where(x => x.Key.AlertStatus.Trim().ToLower() != "ok")
                            .OrderByDescending(x => x.Count())
                            .Select(x =>
                                $"{x.First().AlertName};{x.Key.AlertStatus};{x.Count()};[{x.Key.AlertId}](https://moira.skbkontur.ru/trigger/{x.Key.AlertId})")));

            return client.SendTextMessageAsync(chatId, message, ParseMode.Markdown);
        }

        private Task SaveMessageIfAlertAsync([NotNull] ChatDto chatHistory, long chatId)
        {
            var alerts = GetAlerts(chatHistory.Messages, chatId).ToArray();

            using var queryExecutor = queryExecutorFactory.Create<MoiraAlertHandler, MoiraAlertDbo>();

            queryExecutor.Execute(f => f.Delete(alerts.Select(x => x.StrId).ToArray()));
            queryExecutor.Execute(f => f.Create(alerts));
            return Task.CompletedTask;
        }

        private IEnumerable<MoiraAlertDbo> GetAlerts(MessageDto[] messages, long chatId)
        {
            foreach (var message in messages)
            {
                if (message.From != "Kontur Moira") continue;

                var text = GetText(message.Text);

                yield return new MoiraAlertDbo
                {
                    Text = text,
                    ChatId = chatId,
                    IntId = message.Id,
                    MessageDate = message.Date,
                    Timestamp = DateTime.UtcNow.Ticks,
                    StrId = $"{chatId}_{message.Id}",
                    AlertId = GetAlertId(text),
                    AlertName = GetAlertName(text),
                    AlertStatus = GetAlertStatus(text),
                    AlertText = GetAlertText(text),
                };
            }
        }

        [NotNull]
        private string GetText(object messageText)
        {
            var sb = new StringBuilder();

            var jTokens = (messageText as JArray)?.ToArray() ?? new[] { messageText as JToken };

            foreach (var token in jTokens.Where(x => x != null))
            {
                if (token.Type == JTokenType.String)
                {
                    sb.Append(token);
                    continue;
                }

                var t = token.SelectToken("text");
                if (t.Type == JTokenType.String)
                {
                    sb.Append(t);
                }
            }

            return sb.ToString();
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