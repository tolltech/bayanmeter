using log4net;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Tolltech.TelegramCore;

namespace Tolltech.Counter;

public class CounterBotDaemon(ICounterService counterService)
    : IBotDaemon
{
    private static readonly ILog log = LogManager.GetLogger(typeof(CounterBotDaemon));

    public async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message)
            return;

        try
        {
            var message = update.Message;
            if (message == null)
            {
                return;
            }

            var chatId = message.Chat.Id;
            log.Info($"ReceiveMessage {chatId} {message.MessageId}");

            var messageText = message.Text ?? string.Empty;

            if (messageText.StartsWith("/"))
            {
                if (messageText.StartsWith("/scores"))
                {
                    var counters = await counterService.GetCounters(chatId);
                    var msg = string.Join(Environment.NewLine, counters.Select(x => $"{x.Username} {x.Score}"));
                    await client.SendTextMessageAsync(chatId, msg,
                        cancellationToken: cancellationToken, replyToMessageId: message.MessageId);
                }
                else if (messageText.StartsWith("/my_score"))
                {
                    var fromUserName = message.From?.Username;
                    if (fromUserName == null)
                    {
                        return;
                    }

                    await SendUserScore(client, cancellationToken, fromUserName, chatId, message.MessageId);
                }

                return;
            }

            if (int.TryParse(messageText.Trim(), out var score))
            {
                var fromUsername = message.From?.Username;
                if (fromUsername == null) return;

                log.Info($"Increment {fromUsername} {chatId} by {score}");
                await counterService.Increment(fromUsername, chatId, score);
                await SendUserScore(client, cancellationToken, fromUsername, chatId, message.MessageId);
                return;
            }

            if (!messageText.StartsWith("@")) return;

            var words = messageText.Split([" "], StringSplitOptions.RemoveEmptyEntries);

            if (words.Length > 2 || words.Length < 1) return;

            var userName = words[0].Substring(1);
            if (string.IsNullOrWhiteSpace(userName)) return;

            if (words.Length == 1)
            {
                await SendUserScore(client, cancellationToken, userName, chatId, message.MessageId);
            }

            if (words.Length != 2) return;
            if (!int.TryParse(words[1], out var number)) return;

            log.Info($"Increment {userName} {chatId} by {number}");
            await counterService.Increment(userName, chatId, number);
            await SendUserScore(client, cancellationToken, userName, chatId, message.MessageId);
        }
        catch (Exception e)
        {
            log.Error("BotDaemonException", e);
            Console.WriteLine($"BotDaemonException: {e.Message} {e.StackTrace}");
            if (update.Message?.Chat.Id != null)
                await client.SendTextMessageAsync(update.Message.Chat.Id, "Exception!",
                    cancellationToken: cancellationToken);
        }
    }

    private async Task SendUserScore(ITelegramBotClient client, CancellationToken cancellationToken, string userName,
        long chatId, int messageId)
    {
        var userScoreText = await BuildUserScoreText(userName, chatId);
        await client.SendTextMessageAsync(chatId, userScoreText, cancellationToken: cancellationToken,
            replyToMessageId: messageId);
    }

    private async Task<string> BuildUserScoreText(string userName, long chatId)
    {
        var counter = await counterService.GetCounter(userName, chatId);
        var userScoreText = $"@{userName} score is {counter ?? 0}";
        return userScoreText;
    }

    public Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        log.Error("BotDaemonException", exception);
        Console.WriteLine($"BotDaemonException: {errorMessage} {exception.StackTrace}");
        return Task.CompletedTask;
    }
}