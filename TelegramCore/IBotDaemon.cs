using Telegram.Bot.Args;

namespace Tolltech.TelegramCore
{
    public interface IBotDaemon
    {
        void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs);
    }
}