using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolltech.BayanMeterLib.Psql
{
    [Table("messages")]
    public class MessageDbo
    {
        [Column("str_id", TypeName = "varchar"), Key, Required]
        public string StrId { get; set; }

        [Column("chat_id", TypeName = "bigint"), Required]
        public long ChatId { get; set; }

        [Column("text", TypeName = "varchar")] 
        public string Text { get; set; }

        [Column("forward_from_chat_name", TypeName = "varchar")]
        public string ForwardFromChatName { get; set; }

        [Column("edit_date", TypeName = "timestamp")]
        public DateTime? EditDate { get; set; }

        [Column("forward_from_message_id", TypeName = "int"), Required]
        public int ForwardFromMessageId { get; set; }

        [Column("from_user_id", TypeName = "int"), Required]
        public int FromUserId { get; set; }

        [Column("forward_from_user_id", TypeName = "int")]
        public int? ForwardFromUserId { get; set; }

        [Column("create_date", TypeName = "timestamp"), Required]
        public DateTime CreateDate { get; set; }

        [Column("forward_from_user_name", TypeName = "varchar")]
        public string ForwardFromUserName { get; set; }

        [Column("forward_from_chat_id", TypeName = "bigint")]
        public long? ForwardFromChatId { get; set; }

        [Column("from_user_name", TypeName = "varchar")]
        public string FromUserName { get; set; }

        [Column("int_id", TypeName = "int"), Required]
        public int IntId { get; set; }

        [Column("message_date", TypeName = "timestamp"), Required]
        public DateTime MessageDate { get; set; }

        [Column("timestamp", TypeName = "bigint")]
        public long Timestamp { get; set; }

        [Column("hash", TypeName = "varchar"), Required]
        public string Hash { get; set; }

        [Column("bayan_count", TypeName = "int"), Required]
        public int BayanCount { get; set; }

        [Column("previous_message_id", TypeName = "int")]
        public int PreviousMessageId { get; set; }
    }
}