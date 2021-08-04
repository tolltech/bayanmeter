using System.IO;
using System.Linq;
using System.Reflection;
using Ninject;
using NUnit.Framework;
using Telegram.Bot;
using Tolltech.CheQueue.Psql;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;

namespace Tolltech.CheQueueTest
{
    [TestFixture]
    public abstract class TestBase
    {
        protected StandardKernel kernel;

        protected string WorkDirectoryPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        [SetUp]
        public virtual void Setup()
        {
            var argsFileName = Path.Combine(WorkDirectoryPath, "args.txt");
            var token = File.Exists(argsFileName)
                ? File.ReadAllLines(argsFileName).FirstOrDefault()
                : string.Empty;

            var connectionString = File.Exists(argsFileName)
                ? File.ReadAllLines(argsFileName).Skip(1).FirstOrDefault()
                : string.Empty;

            kernel = new StandardKernel(new ConfigurationModule());;
            var client = new TelegramBotClient(token);
            kernel.Bind<TelegramBotClient>().ToConstant(client);
            kernel.Rebind<IConnectionString>().ToConstant(new ConnectionString(connectionString));
        }
    }
}