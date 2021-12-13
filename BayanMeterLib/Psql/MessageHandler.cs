using System;
using System.Linq;
using JetBrains.Annotations;
using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace Tolltech.BayanMeterLib.Psql
{
    public class MessageHandler : SqlHandlerBase<MessageDbo>
    {
        private readonly DataContextBase<MessageDbo> dataContext;

        public MessageHandler(DataContextBase<MessageDbo> dataContext)
        {
            this.dataContext = dataContext;
        }

        public void Create([NotNull] [ItemNotNull] params MessageDbo[] messages)
        {
            dataContext.Table.AddRange(messages);
        }

        [NotNull]
        [ItemNotNull]
        public MessageDbo[] Select(string[] strIds)
        {
            return dataContext.Table.Where(x => strIds.Contains(x.StrId)).ToArray();
        }

        [NotNull]
        [ItemNotNull]
        public MessageDbo[] Select(long chatId, DateTime fromDate, DateTime toDate)
        {
            return dataContext.Table
                .Where(x => x.ChatId == chatId)
                .Where(x => x.MessageDate >= fromDate && x.MessageDate <= toDate)
                .OrderBy(x => x.MessageDate)
                .ToArray();
        }

        private static readonly Random rnd = new Random();

        [CanBeNull]
        public MessageDbo GetRandom(long chatId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = dataContext.Table
                .Where(x => x.ChatId == chatId);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.MessageDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.MessageDate <= toDate.Value);
            }

            var count = query.Count();

            var number = rnd.Next(count - 1);

            return query
                .OrderBy(x => x.MessageDate)
                .Skip(number)
                .FirstOrDefault();
        }

        public void Update()
        {
            dataContext.SaveChanges();
        }

        [CanBeNull]
        public MessageDbo Find(string messageStrId)
        {
            return dataContext.Table.FirstOrDefault(x => x.StrId == messageStrId);
        }
    }
}