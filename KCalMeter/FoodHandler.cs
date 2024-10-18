using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace KCalMeter;

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
}