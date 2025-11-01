using Microsoft.EntityFrameworkCore;

namespace Tolltech.Planny;

public class PlanService(IDataContextFactory dbContextFactory) : IPlanService
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

    public async Task<PlanDbo[]> SelectAll(int count = 1000)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Plans.OrderByDescending(x => x.Timestamp).Take(count).ToArrayAsync();
    }

    public async Task<PlanDbo[]> SelectByChatId(long chatId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Plans.Where(x => x.ChatId == chatId).OrderByDescending(x => x.Timestamp).ToArrayAsync();
    }

    public async Task<PlanDbo?> DeleteByIdOrChatAndName(int intId, long chatId, string name)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var existent =
            await context.Plans.FirstOrDefaultAsync(x => (x.IntId == intId || x.Name == name) && x.ChatId == chatId);
        if (existent == null)
        {
            return null;
        }

        context.Plans.Remove(existent);
        await context.SaveChangesAsync();
        return existent;
    }

    public async Task<PlanDbo?> DeleteLastByChat(long chatId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var existent = await context.Plans
            .Where(x => x.ChatId == chatId)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();
        
        if (existent == null)
        {
            return null;
        }

        context.Plans.Remove(existent);
        await context.SaveChangesAsync();
        return existent;
    }
}