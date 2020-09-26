using Telegram.Bot.Args;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public interface IBotDaemon
    {
        void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs);
    }
}