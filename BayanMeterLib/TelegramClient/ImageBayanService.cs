using JetBrains.Annotations;
using Tolltech.BayanMeterLib.Psql;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class ImageBayanService : IImageBayanService
    {
        private readonly MessageHandler messageHandler;

        public ImageBayanService(MessageHandler messageHandler)
        {
            this.messageHandler = messageHandler;
        }

        public void CreateMessage(MessageDto message)
        {
            var toCreate = Convert(message);
            messageHandler.Create(toCreate);
        }

        private MessageDbo Convert([NotNull] MessageDto from, [CanBeNull] MessageDbo to = null)
        {
            var result = to ?? new MessageDbo();

            result.StrId = from.StrId;
            result.Text = from.Text;
            result.ForwardFromChatName = from.ForwardFromChatName;
            result.EditDate = from.EditDate;
            result.ForwardFromMessageId = from.ForwardFromMessageId;
            result.ChatId = from.ChatId;
            result.FromUserId = from.FromUserId;
            result.ForwardFromUserId = from.ForwardFromUserId;
            result.CreateDate = from.CreateDate;
            result.ForwardFromUserName = from.ForwardFromUserName;
            result.ForwardFromChatId = from.ForwardFromChatId;
            result.FromUserName = from.FromUserName;
            result.IntId = from.IntId;
            result.MessageDate = from.MessageDate;
            result.Timestamp = from.Timestamp;
            result.Hash = string.Empty;
            result.BayanCount = to?.BayanCount ?? 0;

            return result;
        }
    }
}