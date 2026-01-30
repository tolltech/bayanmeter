namespace Tolltech.BayanMeterLib.Psql
{
    public class ReactionDbo
    {
        public required string TextOrId { get; set; }
        public long? FromUser { get; set; }
        public int Count { get; set; }
    }
}