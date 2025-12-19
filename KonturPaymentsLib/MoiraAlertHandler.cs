using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Tolltech.KonturPaymentsLib
{
    public class MoiraAlertContext : DbContext
    {
        public MoiraAlertContext(DbContextOptions<MoiraAlertContext> options) : base(options)
        {
        }

        public DbSet<MoiraAlertDbo> Table { get; set; }
    }
    
    
    public class MoiraAlertHandler
    {
        private readonly IDbContextFactory<MoiraAlertContext> dbContextFactory;

        public MoiraAlertHandler(IDbContextFactory<MoiraAlertContext> dbContextFactory)
        {
            this.dbContextFactory = dbContextFactory;
        }

        public int Delete(string[] ids)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            var toDelete = dataContext.Table
                .Where(x => ids.Contains(x.StrId))
                .ToArray();

            dataContext.Remove(toDelete);
            dataContext.SaveChanges();
            return toDelete.Length;
        }

        public void Create([NotNull] [ItemNotNull] params MoiraAlertDbo[] alerts)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            dataContext.Table.AddRange(alerts);
        }

        public long GetLastTimestamp()
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            return dataContext.Table.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();
        }

        public MoiraAlertDbo[] Select(long exclusiveFromUtcTicks, long chatId, long? exclusiveToTicks = null)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            var from = new DateTime(exclusiveFromUtcTicks);
            var query = dataContext.Table
                .Where(x => x.MessageDate > from)
                .Where(x => x.ChatId == chatId);

            if (exclusiveToTicks.HasValue)
            {
                var exclusiveToDate = new DateTime(exclusiveToTicks.Value);
                query = query.Where(x => x.MessageDate < exclusiveToDate);
            }

            return query.ToArray();
        }
    }
}