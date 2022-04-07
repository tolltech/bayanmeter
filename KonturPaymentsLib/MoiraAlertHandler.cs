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

        public MoiraAlertDbo[] Select(long exclusiveFromUtcTicks, long chatId)
        {
            var from = new DateTime(exclusiveFromUtcTicks);
            return dataContext.Table
                .Where(x => x.MessageDate > from)
                .Where(x => x.ChatId == chatId)
                .ToArray();
        }
    }
}