using System;
using System.IO;
using System.Linq;
using Ninject;
using Telegram.Bot;
using Tolltech.BayanMeter.Psql;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;

namespace Tolltech.BayanMeter
{
    class Program
    {
        private static TelegramBotClient client;

        static void Main(string[] args)
        {
            Console.WriteLine($"Start BayanMeter {DateTime.Now}");

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
            client = new TelegramBotClient(token);
            kernel.Bind<TelegramBotClient>().ToConstant(client);
            kernel.Rebind<IConnectionString>().ToConstant(new ConnectionString(connectionString));

            var botDaemon = kernel.Get<IBotDaemon>();
            try
            {
                client.OnMessage += botDaemon.BotOnMessageReceived;
                client.OnMessageEdited += botDaemon.BotOnMessageReceived;
                client.StartReceiving();
                Console.ReadLine();
            }
            finally
            {
                client.StopReceiving();
                Console.WriteLine($"End BayanMeter {DateTime.Now}");
            }
        }
    }
}