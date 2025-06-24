using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolltech.Counter;

[Table("counters")]
public class CounterDbo
{
    [Column("id", TypeName = "varchar"), Key, Required]
    public string Id { get; set; }

    [Column("user_name", TypeName = "varchar"), Required]
    public required string UserName { get; set; }

    [Column("chat_id", TypeName = "bigint"), Required]
    public long ChatId { get; set; }

    [Column("counter", TypeName = "int"), Required]
    public int Counter { get; set; }
    
    [Column("timestamp", TypeName = "bigint"), Required]
    public long Timestamp { get; set; }
    
    public static string GetId(string userName, long chatId) => $"{userName}_{chatId}";
}