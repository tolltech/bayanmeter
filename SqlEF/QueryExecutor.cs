using System;
using Tolltech.SqlEF.Integration;

namespace Tolltech.SqlEF
{
    public class QueryExecutor<TSqlHandler, TSqlEntity> : IDisposable where TSqlHandler : SqlHandlerBase<TSqlEntity> where TSqlEntity : class
    {
        private readonly DataContextBase<TSqlEntity> dataContext;
        private readonly ISqlHandlerProvider sqlHandlerProvider;

        public QueryExecutor(DataContextBase<TSqlEntity> dataContext, ISqlHandlerProvider sqlHandlerProvider)
        {
            this.dataContext = dataContext;
            this.sqlHandlerProvider = sqlHandlerProvider;
        }

        public void Execute(Action<TSqlHandler> query)
        {
            var handle = sqlHandlerProvider.Create<TSqlHandler, TSqlEntity>(dataContext);
            query(handle);
            dataContext.SaveChanges();
        }

        public TResult Execute<TResult>(Func<TSqlHandler, TResult> query)
        {
            var handle = sqlHandlerProvider.Create<TSqlHandler, TSqlEntity>(dataContext);
            var result = query(handle);
            dataContext.SaveChanges();
            return result;
        }

        public void Dispose()
        {
            dataContext.Dispose();
        }
    }
}