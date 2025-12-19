using Microsoft.EntityFrameworkCore;

namespace Tolltech.KCalMeter;

public class FoodContext(DbContextOptions<FoodContext> options) : DbContext(options)
{
    public DbSet<FoodDbo> Table { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class FoodHandler(IDbContextFactory<FoodContext> dbContextFactory)
{
    public FoodDbo ReadFood(string name, long chatId, long userId)
    {
        var key = FoodDbo.GetId(name, chatId, userId);
        var result = Find(name, chatId, userId);
        if (result == null)
        {
            throw new ArgumentException($"Не найден объект с ключом {key}");
        }

        return result;
    }

    public void Create(params FoodDbo[] items)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        dataContext.Table.AddRange(items);
        dataContext.SaveChanges();
    }

    public void Delete(params FoodDbo[] items)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        var keys = items.Select(x => x.Id).Distinct().ToArray();
        var toDelete = dataContext.Table.Where(x => keys.Contains(x.Id));
        dataContext.Table.RemoveRange(toDelete);
        dataContext.SaveChanges();
    }

    public FoodDbo? Find(string name, long chatId, long userId)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        var key = FoodDbo.GetId(name, chatId, userId);
        var result = dataContext.Table.FirstOrDefault(x => x.Id == key);
        return result;
    }

    public FoodDbo[] SelectLast(int count, long chatId, long userId, string? sub)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        var query = dataContext.Table.Where(x => x.ChatId == chatId && x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(sub))
        {
            query = query.Where(x => x.Name.Contains(sub));
        }

        return query
            .OrderByDescending(x => x.Timestamp)
            .Take(count)
            .ToArray();
    }
}

public class FoodMessageContext(DbContextOptions<FoodContext> options) : DbContext(options)
{
    public DbSet<FoodMessageDbo> Table { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class FoodMessageHandler(IDbContextFactory<FoodMessageContext> dbContextFactory)
{
    public void Create(params FoodMessageDbo[] items)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        dataContext.Table.AddRange(items);
        dataContext.SaveChanges();
    }

    public FoodMessageDbo[] SelectLast(int count, long chatId, long userId)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        return dataContext.Table
            .Where(x => x.ChatId == chatId && x.UserId == userId)
            .OrderByDescending(x => x.MessageDate)
            .Take(count)
            .ToArray();
    }

    public FoodMessageDbo[] SelectFromDate(DateTime fromDate, long chatId, long userId)
    {
        using var dataContext = dbContextFactory.CreateDbContext();
        return dataContext.Table
            .Where(x => x.MessageDate >= fromDate)
            .Where(x => x.ChatId == chatId && x.UserId == userId)
            .ToArray();
    }
}