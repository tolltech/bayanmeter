using Tolltech.PostgreEF.Integration;

namespace Tolltech.Runner.Psql
{
    public class ConnectionString(string connectionString) : IConnectionString
    {
        public string Value { get; } = connectionString;
    }
}