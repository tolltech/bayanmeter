namespace Tolltech.Planny;

public interface IPlanService
{
    Task CreateOrUpdateByNameAndChat(params PlanDbo[] plans);
}