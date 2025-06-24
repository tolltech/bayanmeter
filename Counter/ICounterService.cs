namespace Tolltech.Counter;

public interface ICounterService
{
    Task Increment(string userName, long chatId, int delta);
}