using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Tolltech.BayanMeterLib.Psql
{
    public class MessageContext : DbContext
    {
        public MessageContext(DbContextOptions<MessageContext> options) : base(options)
        {
        }

        public DbSet<MessageDbo> Table { get; set; }
    }
    
    public class MessageHandler
    {
        private readonly IDbContextFactory<MessageContext> dbContextFactory;

        public MessageHandler(IDbContextFactory<MessageContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
        }

        public void Create([NotNull] [ItemNotNull] params MessageDbo[] messages)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            dataContext.Table.AddRange(messages);
            dataContext.SaveChanges();
        }

        [NotNull]
        [ItemNotNull]
        public MessageDbo[] Select(string[] strIds)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            return dataContext.Table.Where(x => strIds.Contains(x.StrId)).ToArray();
        }

        [NotNull]
        [ItemNotNull]
        public MessageDbo[] Select(long chatId, DateTime fromDate, DateTime toDate)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
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
            using var dataContext = dbContextFactory.CreateDbContext();
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

        [CanBeNull]
        public MessageDbo Find(string messageStrId)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            return dataContext.Table.FirstOrDefault(x => x.StrId == messageStrId);
        }
    }
}