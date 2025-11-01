namespace Tolltech.Planny;

public interface IChatSettingsService
{
    Task<ChatSettingsDbo?> Get(long chatId);
    Task CreateOrUpdate(ChatSettingsDbo newChat);
}