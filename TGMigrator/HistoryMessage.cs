using Newtonsoft.Json;
using Tolltech.BayanMeterLib.Psql;

namespace TGMigrator;

public class HistoryMessage
{
    [JsonProperty("id")] public int? MessageId { get; set; }

    [JsonProperty("type")] public string? Type { get; set; }

    [JsonProperty("reactions")] public Reaction[] Reactions { get; set; } = [];

    public IEnumerable<ReactionDbo> GetReactions()
    {
        foreach (var reaction in Reactions)
        {
            foreach (var user in reaction.FromUsers)
            {
                yield return new ReactionDbo
                {
                    TextOrId = reaction.Emoji ?? reaction.DocumentId ?? "no",
                    FromUser = long.TryParse(new string(user.FromId.Where(char.IsDigit).ToArray()), out var r) ? r : 0L,
                    Count = 1
                };
            }

            var rest = reaction.Count - reaction.FromUsers.Length;
            if (rest > 0)
            {
                yield return new ReactionDbo
                {
                    TextOrId = reaction.Emoji ?? reaction.DocumentId ?? "no",
                    FromUser = null,
                    Count = rest.Value
                };
            }
        }
    }
}

public class Reaction
{
    [JsonProperty("type")] public string? Type { get; set; }

    [JsonProperty("count")] public int? Count { get; set; }

    [JsonProperty("emoji")] public string? Emoji { get; set; }

    [JsonProperty("document_id")] public string? DocumentId { get; set; }

    [JsonProperty("recent")] public FromUser[] FromUsers { get; set; } = [];
}

public class FromUser
{
    [JsonProperty("from_id")] public string FromId { get; set; }
}


//"type": "emoji",
// "count": 3,
// "emoji": "👍",
// "recent": [
//  {
//   "from": "Станислав Федянин",
//   "from_id": "user99886481",
//   "date": "2021-12-30T23:05:47"
//  },