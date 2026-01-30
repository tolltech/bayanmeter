using System.Linq;
using System.Threading.Tasks;
using Tolltech.BayanMeterLib.Psql;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class ImageBayanService(MessageHandler messageHandler) : IImageBayanService
    {
        public void CreateMessage(MessageDto message)
        {
            var toCreate = Convert(message);
            messageHandler.Create(toCreate);
        }

        public async Task UpdateReactions(int messageId, long chatId, long fromUserId, string[] reactions)
        {
            var strId = MessageHelper.GetStrId(chatId, messageId);
            var message = await messageHandler.Find(strId);
            if (message == null) return;

            var newReactions = message.Reactions
                .Where(x => x.FromUser != fromUserId)
                .Concat(reactions.Select(x => new ReactionDbo
                {
                    TextOrId = x,
                    FromUser = fromUserId,
                    Count = 1
                }))
                .ToArray();
            
            await messageHandler.UpdateReactions(strId, newReactions);
        }

        private MessageDbo Convert(MessageDto from)
        {
            var result = new MessageDbo
            {
                StrId = from.StrId,
                Text = from.Text,
                ForwardFromChatName = from.ForwardFromChatName,
                EditDate = from.EditDate,
                ForwardFromMessageId = from.ForwardFromMessageId,
                ChatId = from.ChatId,
                FromUserId = from.FromUserId,
                ForwardFromUserId = from.ForwardFromUserId,
                CreateDate = from.CreateDate,
                ForwardFromUserName = from.ForwardFromUserName,
                ForwardFromChatId = from.ForwardFromChatId,
                FromUserName = from.FromUserName,
                IntId = from.IntId,
                MessageDate = from.MessageDate,
                Timestamp = from.Timestamp,
                Hash = string.Empty,
                BayanCount = 0,
            };

            return result;
        }
    }
}