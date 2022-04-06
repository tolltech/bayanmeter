using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolltech.KonturPaymentsLib
{
    [Table("moira_alerts")]
    public class MoiraAlertDbo
    {
        [Column("str_id", TypeName = "varchar"), Key, Required]
        public string StrId { get; set; }

        [Column("chat_id", TypeName = "bigint"), Required]
        public long ChatId { get; set; }

        [Column("text", TypeName = "varchar")] 
        public string Text { get; set; }

        [Column("int_id", TypeName = "int"), Required]
        public int IntId { get; set; }

        [Column("message_date", TypeName = "timestamp"), Required]
        public DateTime MessageDate { get; set; }

        [Column("timestamp", TypeName = "bigint"), Required]
        public long Timestamp { get; set; }

        [Column("alert_status", TypeName = "varchar")] 
        public string AlertStatus { get; set; }

        [Column("alert_name", TypeName = "varchar")] 
        public string AlertName { get; set; }

        [Column("alert_text", TypeName = "varchar")] 
        public string AlertText { get; set; }

        [Column("alert_id", TypeName = "varchar")] 
        public string AlertId { get; set; }
    }
}