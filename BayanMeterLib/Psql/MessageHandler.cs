using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Tolltech.BayanMeterLib.Psql
{
    public class MessageContext(DbContextOptions<MessageContext> options) : DbContext(options)
    {
        public DbSet<MessageDbo> Table { get; set; }
    }

    public class MessageHandler(IDbContextFactory<MessageContext> dbContextFactory)
    {
        public void Create(params MessageDbo[] messages)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            dataContext.Table.AddRange(messages);
            dataContext.SaveChanges();
        }

        public MessageDbo[] Select(string[] strIds)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            return dataContext.Table.Where(x => strIds.Contains(x.StrId)).ToArray();
        }

        public MessageDbo[] Select(long chatId, DateTime fromDate, DateTime toDate)
        {
            using var dataContext = dbContextFactory.CreateDbContext();
            return dataContext.Table
                .Where(x => x.ChatId == chatId)
                .Where(x => x.MessageDate >= fromDate && x.MessageDate <= toDate)
                .OrderBy(x => x.MessageDate)
                .ToArray();
        }

        public MessageDbo? GetRandom(long chatId, bool withReactions = false, DateTime? fromDate = null, DateTime? toDate = null)
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

            if (withReactions)
            {
                query = query.Where(x => x.ReactionsCount > 0);
            }

            var count = query.Count();

            var number = Random.Shared.Next(count - 1);

            return query
                .OrderBy(x => x.MessageDate)
                .Skip(number)
                .FirstOrDefault();
        }

        public async Task<MessageDbo?> Find(string messageStrId)
        {
            await using var dataContext = await dbContextFactory.CreateDbContextAsync();
            return await dataContext.Table.FirstOrDefaultAsync(x => x.StrId == messageStrId);
        }

        public async Task UpdateReactions(string strId, ReactionDbo[] newReactions, int? customReactionsCount = null)
        {
            await using var dataContext = await dbContextFactory.CreateDbContextAsync();
            var message = await dataContext.Table.FirstAsync(x => x.StrId == strId);

            message.ReactionsCount = customReactionsCount ?? newReactions.Length;
            message.Reactions = newReactions;

            await dataContext.SaveChangesAsync();
        }

        public async Task<MessageDbo[]> GetTopReactionsMessages(long chatId, int count = 3)
        {
            await using var dataContext = await dbContextFactory.CreateDbContextAsync();
            return await dataContext.Table.Where(x => x.ChatId == chatId)
                .OrderByDescending(x => x.ReactionsCount)
                .Take(count)
                .ToArrayAsync();
        }

        public async Task<MessageDbo[]> GetTopReactionsMessages(long chatId, long userId, int count = 3)
        {
            await using var dataContext = await dbContextFactory.CreateDbContextAsync();
            return await dataContext.Table
                .Where(x => x.ChatId == chatId)
                .Where(x => x.FromUserId == userId)
                .OrderByDescending(x => x.ReactionsCount)
                .Take(count)
                .ToArrayAsync();
        }

        public async Task<(MessageDbo LastMessage, MessageDbo MostReactedMessage, int Count)[]> GetTopAuthorsByMessages(
            long chatId, int count = 3)
        {
            await using var dataContext = await dbContextFactory.CreateDbContextAsync();
            var result = await dataContext.Table.Where(x => x.ChatId == chatId)
                .GroupBy(x => x.FromUserId)
                .OrderByDescending(x => x.Count())
                .Select(x => new
                {
                    LastMessage = x.OrderByDescending(x => x.MessageDate).First(),
                    MostReactedMessage = x.OrderByDescending(x => x.ReactionsCount).First(),
                    Count = x.Count()
                })
                .Take(count)
                .ToArrayAsync();

            return result.Select(x => (x.LastMessage, x.MostReactedMessage, x.Count)).ToArray();
        }
    }
}