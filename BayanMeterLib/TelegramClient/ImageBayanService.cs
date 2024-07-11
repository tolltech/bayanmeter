﻿using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Tolltech.BayanMeterLib.Psql;
using Tolltech.SqlEF;

namespace Tolltech.BayanMeterLib.TelegramClient
{
    public class ImageBayanService : IImageBayanService
    {
        private readonly IQueryExecutorFactory queryExecutorFactory;
        private readonly IImageHasher imageHasher;

        public ImageBayanService(IQueryExecutorFactory queryExecutorFactory, IImageHasher imageHasher)
        {
            this.queryExecutorFactory = queryExecutorFactory;
            this.imageHasher = imageHasher;
        }

        public void SaveMessage(params MessageDto[] messages)
        {
            var msgByStrId = messages.GroupBy(x => x.StrId).Select(x => x.First()).ToDictionary(x => x.StrId);

            using var queryExecutor = queryExecutorFactory.Create<MessageHandler, MessageDbo>();
            var existents = queryExecutor.Execute(x => x.Select(msgByStrId.Keys.ToArray()));

            foreach (var existent in existents)
            {
                Convert(msgByStrId[existent.StrId], existent);
            }

            var existentStrIds = new HashSet<string>(existents.Select(x => x.StrId));
            var toCreate = msgByStrId.Values.Where(x => !existentStrIds.Contains(x.StrId)).Select(x => Convert(x))
                .ToArray();

            queryExecutor.Execute(x => x.Create(toCreate));
            queryExecutor.Execute(x => x.Update());
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
            result.Hash = to?.Hash ?? imageHasher.GetHash(from.ImageBytes);
            result.BayanCount = to?.BayanCount ?? 0;

            return result;
        }

        public BayanResultDto GetBayanMetric(string messageStrId)
        {
            using var queryExecutor = queryExecutorFactory.Create<MessageHandler, MessageDbo>();

            var message = queryExecutor.Execute(x => x.Find(messageStrId));

            if (message == null)
            {
                return new BayanResultDto
                {
                    AlreadyWasCount = 0
                };
            }

            var toDate = message.MessageDate;
            var fromDate = toDate.AddMonths(-6);
            var previousMessages = queryExecutor.Execute(x => x.Select(message.ChatId, fromDate.DateTime, toDate.DateTime))
                .Where(x => x.StrId != messageStrId)
                .OrderByDescending(x => x.MessageDate.DateTime)
                .ToArray();

            foreach (var previousMessage in previousMessages)
            {
                if (previousMessage.Hash.HashEquals(message.Hash))
                {
                    message.BayanCount = previousMessage.BayanCount + 1;
                    message.PreviousMessageId = previousMessage.IntId;

                    queryExecutor.Execute(x => x.Update());

                    return new BayanResultDto
                    {
                        AlreadyWasCount = message.BayanCount,
                        PreviousMessageId = previousMessage.IntId,
                        PreviousChatId = previousMessage.ChatId
                    };
                }
            }

            return new BayanResultDto
            {
                AlreadyWasCount = 0
            };
        }
    }
}