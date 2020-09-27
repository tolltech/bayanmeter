using Ninject;
using Ninject.Parameters;
using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace Tolltech.BayanMeter.Psql
{
    public class SqlHandlerProvider : ISqlHandlerProvider
    {
        private readonly IKernel kernel;

        public SqlHandlerProvider(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public TSqlHandler Create<TSqlHandler, TSqlEntity>(DataContextBase<TSqlEntity> dataContext) where TSqlHandler : SqlHandlerBase<TSqlEntity> where TSqlEntity : class
        {
            return kernel.Get<TSqlHandler>(new ConstructorArgument("dataContext", dataContext));
        }
    }
}