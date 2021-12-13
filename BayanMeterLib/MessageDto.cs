using System;

namespace Tolltech.BayanMeterLib
{
    public class MessageDto
    {
        public DateTime MessageDate { get; set; }
        public int IntId { get; set; }
        public long ChatId { get; set; }
        public long Timestamp { get; set; }
        public DateTime CreateDate { get; set; }
        public string FromUserName { get; set; }
        public long FromUserId { get; set; }
        public byte[] ImageBytes { get; set; }
        public DateTime? EditDate { get; set; }
        public string StrId { get; set; }
        public string Text { get; set; }
        public long? ForwardFromUserId { get; set; }
        public string ForwardFromUserName { get; set; }
        public long? ForwardFromChatId { get; set; }
        public string ForwardFromChatName { get; set; }
        public long ForwardFromMessageId { get; set; }
    }
}