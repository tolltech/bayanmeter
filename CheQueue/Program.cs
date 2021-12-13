using System;
using System.IO;
using System.Linq;
using System.Threading;
using Ninject;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Tolltech.CheQueue.Psql;
using Tolltech.CheQueueLib;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;
using Tolltech.TelegramCore;

namespace Tolltech.CheQueue
{
    class Program
    {
        private static TelegramBotClient client;

        static void Main(string[] args)
        {
            Console.WriteLine($"Start CheQueue {DateTime.Now}");

            var argsFileName = "args.txt";
            var token = args.FirstOrDefault()
                        ?? (File.Exists(argsFileName)
                            ? File.ReadAllLines(argsFileName).FirstOrDefault()
                            : string.Empty);

            var connectionString = args.Skip(1).FirstOrDefault()
                                   ?? (File.Exists(argsFileName)
                                       ? File.ReadAllLines(argsFileName).Skip(1).FirstOrDefault()
                                       : string.Empty);

            var specialForAnswersChatId = args.Skip(2).FirstOrDefault()
                                          ?? (File.Exists(argsFileName)
                                              ? File.ReadAllLines(argsFileName).Skip(2).FirstOrDefault()
                                              : string.Empty);

            var kernel = new StandardKernel(new ConfigurationModule("log4net.config"));
            client = new TelegramBotClient(token);
            kernel.Bind<TelegramBotClient>().ToConstant(client);
            kernel.Rebind<IConnectionString>().ToConstant(new ConnectionString(connectionString));
            kernel.Rebind<ISettings>().ToConstant(new Settings {SpecialForAnswersChatId = specialForAnswersChatId});

            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };

            var botDaemon = kernel.Get<IBotDaemon>();
            client.StartReceiving(
                botDaemon.HandleUpdateAsync,
                botDaemon.HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            var me = client.GetMeAsync(cts.Token).GetAwaiter().GetResult();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            cts.Cancel();

            Console.WriteLine($"End CheQueue {DateTime.Now}");
        }
    }
}