using System.IO;
using System.Linq;
using System.Reflection;
using Ninject;
using Telegram.Bot;
using Tolltech.BayanMeter.Psql;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;

namespace BayanMeterCoreTest
{
    public abstract class TestBase
    {
        protected readonly StandardKernel kernel;

        protected string WorkDirecoryPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        protected TestBase()
        {
            var argsFileName = Path.Combine(WorkDirecoryPath, "args.txt");
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
    }}