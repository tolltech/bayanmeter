using Microsoft.EntityFrameworkCore;

namespace Tolltech.Counter;

public class CounterContext(DbContextOptions<CounterContext> options) : DbContext(options)
{
    public DbSet<CounterDbo> Table { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class CounterHandler(IDbContextFactory<CounterContext> dbContextFactory)
{
    public CounterDbo ReadFood(string userName, long chatId)
    {
        var key = CounterDbo.GetId(userName, chatId);
        var result = Find(userName, chatId);
        if (result == null)
        {
            throw new ArgumentException($"Не найден объект с ключом {key}");
        }

        return result;
    }

    public void Create(params CounterDbo[] items)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        dataContext.Table.AddRange(items);
        dataContext.SaveChanges();
    }

    public CounterDbo? Find(string userName, long chatId)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        var key = CounterDbo.GetId(userName, chatId);
        var result = dataContext.Table.FirstOrDefault(x => x.Id == key);
        return result;
    }

    public CounterDbo[] Select(long chatId)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        return dataContext.Table.Where(x => x.ChatId == chatId).OrderByDescending(x => x.Counter).ToArray();
    }

    public void UpdateCounter(string existentId, int existentCounter)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        var entity = dataContext.Table.Single(x => x.Id == existentId);
        entity.Counter = existentCounter;
        dataContext.SaveChanges();
    }
}