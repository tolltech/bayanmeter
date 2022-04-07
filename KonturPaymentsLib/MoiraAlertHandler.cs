using System.Linq;
using JetBrains.Annotations;
using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace Tolltech.KonturPaymentsLib
{
    public class MoiraAlertHandler : SqlHandlerBase<MoiraAlertDbo>
    {
        private readonly DataContextBase<MoiraAlertDbo> dataContext;

        public MoiraAlertHandler(DataContextBase<MoiraAlertDbo> dataContext)
        {
            this.dataContext = dataContext;
        }

        public void Delete(string[] ids)
        {
            var toDelete =  dataContext.Table
                .Where(x => ids.Contains(x.StrId))
                .ToArray();

            dataContext.Delete(toDelete);
        }

        public void Create([NotNull] [ItemNotNull] params MoiraAlertDbo[] alerts)
        {
            dataContext.Table.AddRange(alerts);
        }

        public MoiraAlertDbo[] Select(long exclusiveFromUtcTicks, long chatId)
        {
            return dataContext.Table
                .Where(x => x.Timestamp > exclusiveFromUtcTicks)
                .Where(x => x.ChatId == chatId)
                .ToArray();
        }
    }
}