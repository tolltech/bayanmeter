namespace Tolltech.Planny;

public interface IPlanService
{
    Task CreateOrUpdateByNameAndChat(params PlanDbo[] plans);
    Task<PlanDbo[]> SelectAll(int count = 1000);
}