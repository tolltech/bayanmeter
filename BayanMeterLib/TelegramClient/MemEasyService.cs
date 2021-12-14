using Tolltech.BayanMeterLib.Psql;
using Tolltech.SqlEF;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class MemEasyService : IMemEasyService
    {
        private readonly IQueryExecutorFactory queryExecutorFactory;

        public MemEasyService(IQueryExecutorFactory queryExecutorFactory)
        {
            this.queryExecutorFactory = queryExecutorFactory;
        }

        public MessageDbo GetRandomMessages(long chatId)
        {
            using var queryExecutor = queryExecutorFactory.Create<MessageHandler, MessageDbo>();
            return queryExecutor.Execute(f => f.GetRandom(chatId));
        }
    }
}