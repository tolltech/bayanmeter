using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolltech.Planny;

[Table("plans")]
public class PlanDbo
{
    [Column("id", TypeName = "uuid"), Key, Required]
    public Guid Id { get; set; }
    
    [Column("chat_id", TypeName = "bigint"), Required]
    public long ChatId { get; set; }

    [Column("name", TypeName = "varchar")] 
    public required string Name { get; set; }

    [Column("from_message_id", TypeName = "bigint"), Required]
    public long FromMessageId { get; set; }

    [Column("from_user_id", TypeName = "bigint"), Required]
    public long FromUserId { get; set; }
    
    [Column("create_date"), Required]
    public DateTimeOffset CreateDate { get; set; }

    [Column("from_user_name", TypeName = "varchar")]
    public required string FromUserName { get; set; }
    
    [Column("timestamp", TypeName = "bigint")]
    public long Timestamp { get; set; }
    
    [Column("cron", TypeName = "varchar"), Required]
    public required string Cron { get; set; }
    
    [Column("cron_description", TypeName = "varchar"), Required]
    public required string CronDescription { get; set; }
}