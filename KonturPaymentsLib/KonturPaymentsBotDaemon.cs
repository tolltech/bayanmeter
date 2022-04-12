using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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
                SendStatsReportAsync(telegramBotClient, chatId, 1).GetAwaiter().GetResult();
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

                log.Info($"ReceiveMessage {message.Chat.Id} {message.MessageId}");

                if (message.Text?.StartsWith(@"/stats") ?? false)
                {
                    var dayCount = int.TryParse(message.Text.Replace(@"/stats", string.Empty).Trim(), out var d)
                        ? d
                        : 1;
                    await SendStatsReportAsync(client, message.Chat.Id, dayCount).ConfigureAwait(false);
                    return;
                }

                if (message.Text?.StartsWith(@"/diff") ?? false)
                {
                    var parameters = message.Text.Replace(@"/diff", string.Empty)
                        .Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToArray();

                    var fromDate = DateTime.TryParseExact(parameters.FirstOrDefault(),
                        "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)
                        ? d1
                        : DateTime.Now.Date.AddDays(-1);

                    var toDate = DateTime.TryParseExact(parameters.Skip(1).FirstOrDefault(),
                        "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2)
                        ? d2
                        : DateTime.Now.Date;

                    await SendDiffReportAsync(client, message.Chat.Id, fromDate, toDate).ConfigureAwait(false);
                    return;
                }

                var document = message.Document;

                if (document == null || string.IsNullOrWhiteSpace(document.FileId))
                {
                    return;
                }

                var file = telegramClient.GetFile(document.FileId);

                var chatHistory = JsonConvert.DeserializeObject<ChatDto>(Encoding.UTF8.GetString(file));

                chatIds.Add(message.Chat.Id);

                if (timer == null)
                {
                    timer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);

                    timer.Elapsed += (sender, e) => OnTimedEvent();
                    timer.AutoReset = true;
                    timer.Enabled = true;
                }

                var result = await SaveMessageIfAlertAsync(chatHistory, message.Chat.Id).ConfigureAwait(false);

                await client.SendTextMessageAsync(message.Chat.Id,
                        $"``` Total {result.Total}, New {result.Total - result.Deleted} ```", ParseMode.Markdown,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Error("BotDaemonException", e);
                Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            }
        }

        private async Task SendDiffReportAsync(ITelegramBotClient client, long chatId, DateTime fromDate, DateTime toDate)
        {
            using var queryExecutor = queryExecutorFactory.Create<MoiraAlertHandler, MoiraAlertDbo>();

            var todayAlerts = queryExecutor.Execute(f => f.Select(toDate.Ticks, chatId));
            var yesterdayAlerts = queryExecutor.Execute(f => f.Select(fromDate.Ticks, chatId, toDate.Ticks));

            await client.SendTextMessageAsync(chatId, $"``` Diff from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} ```", ParseMode.Markdown).ConfigureAwait(false);

            Console.WriteLine($"diff from {fromDate} to {toDate}. {yesterdayAlerts.Length} {todayAlerts.Length}");

            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine(
                $"Yesterday - Ok {yesterdayAlerts.Count(x => x.AlertStatus.ToLower() == "ok")} NotOk {yesterdayAlerts.Count(x => x.AlertStatus.ToLower() != "ok")}");
            sb.AppendLine(
                $"Today - Ok {todayAlerts.Count(x => x.AlertStatus.ToLower() == "ok")} NotOk {todayAlerts.Count(x => x.AlertStatus.ToLower() != "ok")}");
            sb.AppendLine("```");

            var yesterdayAlertIds = new HashSet<string>(yesterdayAlerts.Select(x => x.AlertId));
            var newTodayAlerts = todayAlerts.Where(x => !yesterdayAlertIds.Contains(x.AlertId)).GroupBy(x => x.AlertId).ToArray();
            sb.AppendLine($"New - {newTodayAlerts.Length}");

            foreach (var alert in newTodayAlerts.OrderByDescending(x=>x.Count()))
            {
                sb.AppendLine($"{alert.Count()};[{alert.First().AlertName}](https://moira.skbkontur.ru/trigger/{alert.Key})");
            }

            var todayNotOkAlertIds = new HashSet<string>(todayAlerts.Where(x => x.AlertStatus.ToLower() != "ok").Select(x => x.AlertId));
            var repairedAlerts = yesterdayAlerts.Where(x => !todayNotOkAlertIds.Contains(x.AlertId)).GroupBy(x=>x.AlertId).ToArray();
            sb.AppendLine($"Repaired - {repairedAlerts.Length}");
            foreach (var alert in repairedAlerts.OrderByDescending(x=>x.Count()))
            {
                sb.AppendLine($"{alert.Count()};[{alert.First().AlertName}](https://moira.skbkontur.ru/trigger/{alert.Key})");
            }

            await client.SendTextMessageAsync(chatId, sb.ToString(), ParseMode.Markdown).ConfigureAwait(false);
        }

        private async Task SendStatsReportAsync(ITelegramBotClient client, long chatId, int dayCount)
        {
            Console.WriteLine($"Send stats for {chatId} days {dayCount}");

            var fromDate = DateTime.UtcNow.AddDays(-dayCount);
            await client.SendTextMessageAsync(chatId, $"``` Stats from {fromDate:s} ```", ParseMode.Markdown).ConfigureAwait(false);

            using var queryExecutor = queryExecutorFactory.Create<MoiraAlertHandler, MoiraAlertDbo>();
            var alerts = queryExecutor.Execute(f => f.Select(fromDate.Ticks, chatId));

            var message = string.Join("\r\n",
                new[] { "Name;Status;Count;Url" }
                    .Concat(
                        alerts.GroupBy(x => (x.AlertId, x.AlertStatus))
                            .Where(x => x.Key.AlertStatus.Trim().ToLower() != "ok")
                            .OrderByDescending(x => x.Count())
                            .Select(x =>
                                $"{x.First().AlertName};{x.Key.AlertStatus};{x.Count()};[{x.Key.AlertId}](https://moira.skbkontur.ru/trigger/{x.Key.AlertId})")));

            await client.SendTextMessageAsync(chatId, message, ParseMode.Markdown).ConfigureAwait(false);
        }

        private Task<(int Deleted, int Total)> SaveMessageIfAlertAsync([NotNull] ChatDto chatHistory, long chatId)
        {
            var alerts = GetAlerts(chatHistory.Messages, chatId).ToArray();

            using var queryExecutor = queryExecutorFactory.Create<MoiraAlertHandler, MoiraAlertDbo>();

            var deletedCount = queryExecutor.Execute(f => f.Delete(alerts.Select(x => x.StrId).ToArray()));
            queryExecutor.Execute(f => f.Create(alerts));
            return Task.FromResult((deletedCount, alerts.Length));
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