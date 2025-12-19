using System.Collections.Generic;
using Tolltech.BayanMeterLib.Psql;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class MemEasyService : IMemEasyService
    {
        private readonly MessageHandler messageHandler;

        public MemEasyService(MessageHandler messageHandler)
        {
            this.messageHandler = messageHandler;
        }

        public MessageDbo GetRandomMessages(long chatId)
        {
            return messageHandler.GetRandom(chatId) ?? throw new KeyNotFoundException();
        }
    }
}