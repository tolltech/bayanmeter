using System.IO;
using System.Linq;
using Ninject;
using Telegram.Bot;
using Tolltech.BayanMeter.Psql;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;

namespace Migration
{
    class Program
    {
        static void Main(string[] args)
        {
            var argsFileName = "args.txt";
            var token = args.FirstOrDefault()
                        ?? (File.Exists(argsFileName)
                            ? File.ReadAllLines(argsFileName).FirstOrDefault()
                            : string.Empty);

            var connectionString = args.Skip(1).FirstOrDefault()
                                   ?? (File.Exists(argsFileName)
                                       ? File.ReadAllLines(argsFileName).Skip(1).FirstOrDefault()
                                       : string.Empty);

            var kernel = new StandardKernel(new ConfigurationModule("log4net.config"));
            var client = new TelegramBotClient(token);
            kernel.Bind<TelegramBotClient>().ToConstant(client);
            kernel.Rebind<IConnectionString>().ToConstant(new ConnectionString(connectionString));

            var imageBayanService = kernel.Get<IImageBayanService>();
            //client.GetChatAsync(new )
        }
    }
}