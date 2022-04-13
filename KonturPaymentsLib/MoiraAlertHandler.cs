using System;
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

        public int Delete(string[] ids)
        {
            var toDelete = dataContext.Table
                .Where(x => ids.Contains(x.StrId))
                .ToArray();

            dataContext.Delete(toDelete);
            return toDelete.Length;
        }

        public void Create([NotNull] [ItemNotNull] params MoiraAlertDbo[] alerts)
        {
            dataContext.Table.AddRange(alerts);
        }

        public long GetLastTimestamp()
        {
            return dataContext.Table.OrderByDescending(x => x.Timestamp).Select(x => x.Timestamp).FirstOrDefault();
        }

        public MoiraAlertDbo[] Select(long exclusiveFromUtcTicks, long chatId, long? exclusiveToTicks = null)
        {
            var from = new DateTime(exclusiveFromUtcTicks);
            var query = dataContext.Table
                .Where(x => x.MessageDate > from)
                .Where(x => x.ChatId == chatId);

            if (exclusiveToTicks.HasValue)
            {
                var exclusiveToDate = new DateTime(exclusiveToTicks.Value);
                query = query.Where(x => x.MessageDate < exclusiveToDate);
            }

            return query.ToArray();
        }
    }
}