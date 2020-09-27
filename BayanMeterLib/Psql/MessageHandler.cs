using System.Linq;
using JetBrains.Annotations;
using Tolltech.SqlEF;
using Tolltech.SqlEF.Integration;

namespace Tolltech.BayanMeterLib.Psql
{
    public class MessageHandler : SqlHandlerBase<MessageDbo>
    {
        private readonly DataContextBase<MessageDbo> dataContext;

        public MessageHandler(DataContextBase<MessageDbo> dataContext)
        {
            this.dataContext = dataContext;
        }

        public void Create([NotNull] [ItemNotNull] params MessageDbo[] messages)
        {
            dataContext.Table.AddRange(messages);
        }

        [NotNull]
        [ItemNotNull]
        public MessageDbo[] Select(string[] strIds)
        {
            return dataContext.Table.Where(x => strIds.Contains(x.StrId)).ToArray();
        }

        public void Update()
        {
            dataContext.SaveChanges();
        }
    }
}