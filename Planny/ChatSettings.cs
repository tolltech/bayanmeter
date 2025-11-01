using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Tolltech.Planny;

[Table("chat_settings")]
public class ChatSettingsDbo
{
    [Column("chat_id", TypeName = "bigint"), Key, Required]
    public long ChatId { get; set; }

    [Column("timestamp", TypeName = "bigint")]
    public long Timestamp { get; set; }

    [Column("settings", TypeName = "varchar"), ConcurrencyCheck, Required]
    public string SettingsDbo { get; set; } = "{}";

    private ChatSettings? chatSettings;

    [NotMapped]
    public ChatSettings Settings
    {
        get => chatSettings ??= JsonConvert.DeserializeObject<ChatSettings>(SettingsDbo) ?? new ChatSettings();
        set => SettingsDbo = JsonConvert.SerializeObject(chatSettings = value);
    }
}

public class ChatSettings
{
    [JsonProperty] public string Locale { get; init; } = string.Empty;
    
    [JsonProperty] public TimeSpan Offset { get; init; } = TimeSpan.Zero;
}