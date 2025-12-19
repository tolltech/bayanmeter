using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace Tolltech.Runner.Psql
{
    public class SqlHandlerProvider : ISqlHandlerProvider
    {
        public TSqlHandler Create<TSqlHandler, TSqlEntity>(DataContextBase<TSqlEntity> dataContext)
            where TSqlHandler : SqlHandlerBase<TSqlEntity> where TSqlEntity : class
        {
            return (TSqlHandler)(Activator.CreateInstance(typeof(TSqlHandler), dataContext)
                                 ?? throw new NotImplementedException(
                                     $"Unable to create SqlHandlerProvider for {typeof(TSqlHandler).Name}"));
        }
    }
}