using Tolltech.SqlEF;

namespace Tolltech.KCalMeter;

public class KCalMeterService : IKCalMeterService
{
    private readonly IQueryExecutorFactory queryExecutorFactory;

    public KCalMeterService(IQueryExecutorFactory queryExecutorFactory)
    {
        this.queryExecutorFactory = queryExecutorFactory;
    }

    public Task WritePortion(string name, int portion, long chatId, long userId, DateTime messageDate)
    {
        using var queryExecutorFood = queryExecutorFactory.Create<FoodHandler, FoodDbo>();
        using var queryExecutorFoodMessage = queryExecutorFactory.Create<FoodMessageHandler, FoodMessageDbo>();

        var food = queryExecutorFood.Execute(f => f.ReadFood(name, chatId, userId));

        var kcal = food.Kcal * portion / food.BasePortion;
        var protein = food.Protein * portion / food.BasePortion;
        var fat = food.Fat * portion / food.BasePortion;
        var carbohydrate = food.Carbohydrate * portion / food.BasePortion;

        var newPortion = new FoodMessageDbo
        {
            Id = Guid.NewGuid(),
            FoodId = food.Id,
            Name = name,
            ChatId = chatId,
            UserId = userId,
            MessageDate = messageDate,
            CreateDate = DateTime.UtcNow,
            Kcal = kcal,
            Protein = protein,
            Fat = fat,
            Carbohydrate = carbohydrate
        };

        queryExecutorFoodMessage.Execute(f => f.Create(newPortion));

        return Task.CompletedTask;
    }

    public Task DeleteFood(string name, long chatId, long userId)
    {
        using var queryExecutorFood = queryExecutorFactory.Create<FoodHandler, FoodDbo>();
        var toDelete = queryExecutorFood.Execute(x => x.Find(name, chatId, userId));

        if (toDelete == null) return Task.CompletedTask;

        queryExecutorFood.Execute(x => x.Delete(toDelete));

        return Task.CompletedTask;
    }

    public Task WriteFood(string name, int basePortion, FoodInfo foodInfo, long chatId, long userId)
    {
        using var queryExecutorFood = queryExecutorFactory.Create<FoodHandler, FoodDbo>();
        var newFood = new FoodDbo
        {
            Id = FoodDbo.GetId(name, chatId, userId),
            Name = name,
            ChatId = chatId,
            UserId = userId,
            Kcal = foodInfo.KCal,
            Protein = foodInfo.Protein,
            Fat = foodInfo.Fat,
            Carbohydrate = foodInfo.Carbohydrates,
            BasePortion = basePortion
        };
        
        return Task.CompletedTask;
    }
}