namespace Tolltech.Counter;

public interface ICounterService
{
    Task Increment(string userName, long chatId, int delta);
    Task<int?> GetCounter(string userName, long chatId);
    Task<(string Username, int Score)[]> GetCounters(long chatId);
}