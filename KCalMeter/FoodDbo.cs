using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolltech.KCalMeter;

[Table("foods")]
public class FoodDbo
{
    [Column("id", TypeName = "varchar"), Key, Required]
    public string Id { get; set; }

    [Column("name", TypeName = "varchar"), Required]
    public string Name { get; set; }

    [Column("chat_id", TypeName = "bigint"), Required]
    public long ChatId { get; set; }

    [Column("user_id", TypeName = "bigint"), Required]
    public long UserId { get; set; }

    [Column("kcal", TypeName = "int"), Required]
    public int Kcal { get; set; }

    [Column("protein", TypeName = "int"), Required]
    public int Protein { get; set; }

    [Column("fat", TypeName = "int"), Required]
    public int Fat { get; set; }

    [Column("carbohydrate", TypeName = "int"), Required]
    public int Carbohydrate { get; set; }
    
    [Column("base_portion", TypeName = "int"), Required]
    public int BasePortion { get; set; }

    [Column("timestamp", TypeName = "bigint"), Required]
    public long Timestamp { get; set; }

    public static string GetId(string name, long chatId, long userId)
    {
        return $"{userId}_{chatId}_{name}";
    }
    
    public string GetId()
    {
        return GetId(Name, ChatId, UserId);
    }
}

[Table("food_messages")]
public class FoodMessageDbo
{
    [Column("id", TypeName = "uuid"), Key, Required]
    public Guid Id { get; set; }

    [Column("food_id", TypeName = "varchar"), Required]
    public string FoodId { get; set; }

    [Column("name", TypeName = "varchar"), Required]
    public string Name { get; set; }

    [Column("chat_id", TypeName = "bigint"), Required]
    public long ChatId { get; set; }

    [Column("user_id", TypeName = "bigint"), Required]
    public long UserId { get; set; }

    [Column("message_date"), Required]
    public DateTimeOffset MessageDate { get; set; }
    
    [Column("create_date"), Required]
    public DateTimeOffset CreateDate { get; set; }

    [Column("kcal", TypeName = "int"), Required]
    public int Kcal { get; set; }

    [Column("protein", TypeName = "int"), Required]
    public int Protein { get; set; }

    [Column("fat", TypeName = "int"), Required]
    public int Fat { get; set; }

    [Column("carbohydrate", TypeName = "int"), Required]
    public int Carbohydrate { get; set; }
}