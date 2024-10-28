using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace Tolltech.KCalMeter;

// ReSharper disable once ClassNeverInstantiated.Global
public class FoodHandler : SqlHandlerBase<FoodDbo>
{
    private readonly DataContextBase<FoodDbo> dataContext;

    public FoodHandler(DataContextBase<FoodDbo> dataContext)
    {
        this.dataContext = dataContext;
    }

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

    public void Update()
    {
        dataContext.SaveChanges();
    }

    public void Create(params FoodDbo[] items)
    {
        dataContext.Table.AddRange(items);
    }

    public void Delete(params FoodDbo[] items)
    {
        var keys = items.Select(x => x.Id).Distinct().ToArray();
        var toDelete = dataContext.Table.Where(x => keys.Contains(x.Id));
        dataContext.Table.RemoveRange(toDelete);
    }

    public FoodDbo? Find(string name, long chatId, long userId)
    {
        var key = FoodDbo.GetId(name, chatId, userId);
        var result = dataContext.Table.FirstOrDefault(x => x.Id == key);
        return result;
    }

    public FoodDbo[] SelectLast(int count, long chatId, long userId, string? sub)
    {
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

// ReSharper disable once ClassNeverInstantiated.Global
public class FoodMessageHandler : SqlHandlerBase<FoodMessageDbo>
{
    private readonly DataContextBase<FoodMessageDbo> dataContext;

    public FoodMessageHandler(DataContextBase<FoodMessageDbo> dataContext)
    {
        this.dataContext = dataContext;
    }

    public void Update()
    {
        dataContext.SaveChanges();
    }

    public void Create(params FoodMessageDbo[] items)
    {
        dataContext.Table.AddRange(items);
    }

    public FoodMessageDbo[] SelectLast(int count, long chatId, long userId)
    {
        return dataContext.Table
            .Where(x => x.ChatId == chatId && x.UserId == userId)
            .OrderByDescending(x => x.MessageDate)
            .Take(count)
            .ToArray();
    }

    public FoodMessageDbo[] SelectFromDate(DateTime fromDate, long chatId, long userId)
    {
        return dataContext.Table
            .Where(x => x.MessageDate >= fromDate)
            .Where(x => x.ChatId == chatId && x.UserId == userId)
            .ToArray();
    }
}