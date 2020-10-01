namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class BayanResultDto
    {
        public int AlreadyWasCount { get; set; }
        public int PreviousMessageId { get; set; }
        public long PreviousChatId { get; set; }
    }
}