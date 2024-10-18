namespace Tolltech.KCalMeter;

public interface IKCalMeterService
{
    Task WritePortion(string name, int portion, long chatId, long userId, DateTime messageDate);
    Task DeleteFood(string name, long chatId, long userId);
    Task WriteFood(string name, int basePortion, FoodInfo foodInfo, long chatId, long userId);
}