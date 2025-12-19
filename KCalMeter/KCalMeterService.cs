
namespace Tolltech.KCalMeter;

public class KCalMeterService(FoodHandler foodHandler, FoodMessageHandler foodMessageHandler) : IKCalMeterService
{
    public Task WritePortion(string name, decimal portion, long chatId, long userId, DateTime messageDate)
    {
        var food = foodHandler.ReadFood(name, chatId, userId);

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
            Kcal = (int)kcal,
            Protein = (int)protein,
            Fat = (int)fat,
            Carbohydrate = (int)carbohydrate
        };

        foodMessageHandler.Create(newPortion);

        return Task.CompletedTask;
    }

    public Task DeleteFood(string name, long chatId, long userId)
    {
        var toDelete = foodHandler.Find(name, chatId, userId);

        if (toDelete == null) return Task.CompletedTask;

        foodHandler.Delete(toDelete);

        return Task.CompletedTask;
    }

    public Task WriteFood(string name, int basePortion, FoodInfo foodInfo, long chatId, long userId)
    {
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

        var toDelete = foodHandler.Find(name, chatId, userId);
        if (toDelete != null)
        {
            foodHandler.Delete(toDelete);
        }

        foodHandler.Create(newFood);

        return Task.CompletedTask;
    }

    public Task<FoodMessageDbo[]> SelectPortions(int count, long chatId, long userId)
    {
        return Task.FromResult(foodMessageHandler.SelectLast(count, chatId, userId));
    }

    public Task<FoodMessageDbo[]> SelectPortions(DateTime fromDate, long chatId, long userId)
    {
        return Task.FromResult(foodMessageHandler.SelectFromDate(fromDate.Date, chatId, userId));
    }

    public Task<FoodDbo[]> SelectFood(int count, long chatId, long userId, string? sub)
    {
        return Task.FromResult(foodHandler.SelectLast(count, chatId, userId, sub));
    }
}