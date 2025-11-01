using Microsoft.EntityFrameworkCore;

namespace Tolltech.Planny;

public class ChatSettingsService(IDataContextFactory dbContextFactory) : IChatSettingsService
{
    public async Task<ChatSettingsDbo?> Get(long chatId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.ChatSettings.FirstOrDefaultAsync(x => x.ChatId == chatId);
    }

    public async Task CreateOrUpdate(ChatSettingsDbo newChat)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var existent = await context.ChatSettings.FirstOrDefaultAsync(x => x.ChatId == newChat.ChatId);
        if (existent != null)
        {
            context.ChatSettings.Remove(existent);
        }

        await context.ChatSettings.AddAsync(newChat);

        await context.SaveChangesAsync();
    }
}