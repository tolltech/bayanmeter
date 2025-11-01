namespace Tolltech.Planny;

public interface IPlanService
{
    Task CreateOrUpdateByNameAndChat(params PlanDbo[] plans);
    Task<PlanDbo[]> SelectAll(int count = 1000);
    Task<PlanDbo[]> SelectByChatId(long chatId);
    Task<PlanDbo?> DeleteByIdOrChatAndName(int intId, long chatId, string name);
    Task<PlanDbo?> DeleteLastByChat(long chatId);
}