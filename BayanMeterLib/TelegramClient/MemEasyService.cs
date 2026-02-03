using System.Collections.Generic;
using Tolltech.BayanMeterLib.Psql;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class MemEasyService(MessageHandler messageHandler) : IMemEasyService
    {
        public MessageDbo GetRandomMessages(long chatId)
        {
            return messageHandler.GetRandom(chatId) ?? throw new KeyNotFoundException();
        }

        public MessageDbo GetRandomTopMessages(long chatId)
        {
            return messageHandler.GetRandom(chatId, true) ?? throw new KeyNotFoundException();
        }
    }
}