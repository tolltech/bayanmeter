using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace Tolltech.Counter;

// ReSharper disable once ClassNeverInstantiated.Global
public class CounterHandler : SqlHandlerBase<CounterDbo>
{
    private readonly DataContextBase<CounterDbo> dataContext;

    public CounterHandler(DataContextBase<CounterDbo> dataContext)
    {
        this.dataContext = dataContext;
    }

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

    public void Update()
    {
        dataContext.SaveChanges();
    }

    public void Create(params CounterDbo[] items)
    {
        dataContext.Table.AddRange(items);
    }

    public CounterDbo? Find(string userName, long chatId)
    {
        var key = CounterDbo.GetId(userName, chatId);
        var result = dataContext.Table.FirstOrDefault(x => x.Id == key);
        return result;
    }
}