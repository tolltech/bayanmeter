using Tolltech.BayanMeterLib.Psql;

namespace TGMigrator;

public class HistoryMessage
{
    public int MessageId { get; set; }
    public long ChatId { get; set; }

    public ReactionDbo[] GetReactions()
    {
        return [];
    }
}