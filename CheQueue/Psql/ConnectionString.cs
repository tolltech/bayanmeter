﻿using Tolltech.PostgreEF.Integration;

namespace Tolltech.CheQueue.Psql
{
    public class ConnectionString : IConnectionString
    {
        public ConnectionString(string connectionString)
        {
            Value = connectionString;
        }

        public string Value { get; }
    }
}