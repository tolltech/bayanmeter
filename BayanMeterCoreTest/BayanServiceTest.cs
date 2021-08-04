using System;
using System.IO;
using System.Linq;
using Ninject;
using Tolltech.BayanMeterLib;
using Tolltech.BayanMeterLib.Psql;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.SqlEF;
using Xunit;

namespace BayanMeterCoreTest
{
    public class BayanServiceTest : TestBase
    {
        private IImageBayanService imageBayanService;
        private IQueryExecutorFactory queryExecutorFactory;

        public BayanServiceTest()
        {
            imageBayanService = kernel.Get<IImageBayanService>();
            queryExecutorFactory = kernel.Get<IQueryExecutorFactory>();
        }

        [Fact]
        public void Simple()
        {
            var inputFilePath = Path.Combine(WorkDirectoryPath, "Images", "test.jpg");
            var messageDto = new MessageDto
            {
                StrId = Guid.NewGuid().ToString(),
                Text = Guid.NewGuid().ToString(),
                ImageBytes = File.ReadAllBytes(inputFilePath),
                EditDate = DateTime.Now,
                ForwardFromMessageId = 42,
                ChatId = 411,
                CreateDate = DateTime.Now,
                ForwardFromChatId = 4444,
                ForwardFromChatName = Guid.NewGuid().ToString(),
                ForwardFromUserId = 3333,
                ForwardFromUserName = Guid.NewGuid().ToString(),
                FromUserId = 322,
                FromUserName = Guid.NewGuid().ToString(),
                IntId = 444556,
                MessageDate = DateTime.Now,
                Timestamp = DateTime.Now.Ticks
            };

            imageBayanService.SaveMessage(messageDto);

            var queryExecutor = queryExecutorFactory.Create<MessageHandler, MessageDbo>();
            var messageDbos = queryExecutor.Execute(x => x.Select(new[] {messageDto.StrId}));
            Assert.NotEmpty(messageDbos);

            var actual = messageDbos.Single();
            AssertEqual(messageDto, actual);
        }

        [Fact]
        public void SimpleWithUpdate()
        {
            var inputFilePath = Path.Combine(WorkDirectoryPath, "Images", "test.jpg");
            var messageDto = new MessageDto
            {
                StrId = Guid.NewGuid().ToString(),
                Text = Guid.NewGuid().ToString(),
                ImageBytes = File.ReadAllBytes(inputFilePath),
                EditDate = DateTime.Now,
                ForwardFromMessageId = 42,
                ChatId = 411,
                CreateDate = DateTime.Now,
                ForwardFromChatId = 4444,
                ForwardFromChatName = Guid.NewGuid().ToString(),
                ForwardFromUserId = 3333,
                ForwardFromUserName = Guid.NewGuid().ToString(),
                FromUserId = 322,
                FromUserName = Guid.NewGuid().ToString(),
                IntId = 444556,
                MessageDate = DateTime.Now,
                Timestamp = DateTime.Now.Ticks
            };

            imageBayanService.SaveMessage(messageDto);

            var queryExecutor = queryExecutorFactory.Create<MessageHandler, MessageDbo>();
            var messageDbos = queryExecutor.Execute(x => x.Select(new[] {messageDto.StrId}));
            //note: without this next queryExecutor read old entity
            queryExecutor.Dispose();

            Assert.NotEmpty(messageDbos);

            var actual = messageDbos.Single();
            AssertEqual(messageDto, actual);

            var messageDto2 = new MessageDto
            {
                StrId = messageDto.StrId,
                Text = Guid.NewGuid().ToString(),
                ImageBytes = File.ReadAllBytes(inputFilePath),
                EditDate = DateTime.Now,
                ForwardFromMessageId = 42,
                ChatId = 411,
                CreateDate = DateTime.Now,
                ForwardFromChatId = 4444,
                ForwardFromChatName = Guid.NewGuid().ToString(),
                ForwardFromUserId = 3333,
                ForwardFromUserName = Guid.NewGuid().ToString(),
                FromUserId = 322,
                FromUserName = Guid.NewGuid().ToString(),
                IntId = 444556,
                MessageDate = DateTime.Now,
                Timestamp = DateTime.Now.Ticks
            };

            imageBayanService.SaveMessage(messageDto2);

            queryExecutor = queryExecutorFactory.Create<MessageHandler, MessageDbo>();
            messageDbos = queryExecutor.Execute(x => x.Select(new[] {messageDto2.StrId}));
            Assert.NotEmpty(messageDbos);

            actual = messageDbos.Single();
            AssertEqual(messageDto2, actual);
        }

        private void AssertEqual(MessageDto expected, MessageDbo actual)
        {
            Assert.Equal(expected.StrId, actual.StrId);
            Assert.Equal(expected.Text, actual.Text);
            Assert.Equal(expected.EditDate?.Date, actual.EditDate?.Date);
            Assert.Equal(expected.ForwardFromMessageId, actual.ForwardFromMessageId);
            Assert.Equal(expected.FromUserName, actual.FromUserName);
            Assert.Equal(expected.IntId, actual.IntId);
            Assert.Equal(expected.ForwardFromChatName, actual.ForwardFromChatName);
            Assert.Equal(expected.ForwardFromUserName, actual.ForwardFromUserName);
            Assert.NotEmpty(actual.Hash);
            Assert.Equal(0, actual.BayanCount);
            Assert.Equal(expected.ChatId, actual.ChatId);
            Assert.Equal(expected.CreateDate.Date, actual.CreateDate.Date);
            Assert.Equal(expected.ForwardFromChatId, actual.ForwardFromChatId);
            Assert.Equal(expected.ForwardFromUserId, actual.ForwardFromUserId);
            Assert.Equal(expected.MessageDate.Date, actual.MessageDate.Date);
            Assert.Equal(expected.Timestamp, actual.Timestamp);
        }
    }
}