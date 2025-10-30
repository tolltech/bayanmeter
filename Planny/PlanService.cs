using Microsoft.EntityFrameworkCore;

namespace Tolltech.Planny;

public class PlanService(IDataContextFactory<PlannyContext> dbContextFactory) : IPlanService
{
    public async Task CreateOrUpdateByNameAndChat(params PlanDbo[] plans)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        foreach (var plan in plans)
        {
            var existent = await context.Plans.FirstOrDefaultAsync(x =>
                x.Name == plan.Name && x.ChatId == plan.ChatId
                || x.Id == plan.Id
            );
            
            if (existent != null)
            {
                context.Plans.Remove(existent);
            }

            await context.Plans.AddAsync(plan);
        }

        await context.SaveChangesAsync();
    }
}