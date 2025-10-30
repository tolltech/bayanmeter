using Tolltech.PostgreEF.Integration;

namespace Tolltech.BotRunner.Psql
{
    public class ConnectionString(string connectionString) : IConnectionString
    {
        public string Value { get; } = connectionString;
    }
}