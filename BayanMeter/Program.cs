using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Ninject;
using Telegram.Bot;
using Tolltech.BayanMeter.Psql;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;
using Tolltech.TelegramCore;
using Telegram.Bot.Extensions.Polling;
using Tolltech.AlertBot;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.CoreLib;
using Tolltech.Counter;
using Tolltech.KCalMeter;
using Tolltech.KonturPaymentsLib;
using Tolltech.LevDimover;
using Tolltech.Planny;
using Tolltech.Storer;

namespace Tolltech.BayanMeter
{
    class Program
    {
        class AppSettings
        {
            public string ConnectionString { get; set; }
            public BotSettings[] BotSettings { get; set; }
        }
        
        class BotSettings
        {
            public string Token { get; set; }
            public string BotName { get; set; }
            public string CustomSettings { get; set; }
        }

        private static TelegramBotClient client;

        static void Main(string[] args)
        {
            Console.WriteLine($"Start Bots {DateTime.Now}");

            var argsFileName = "args.txt";
            var botSettingsStr = args.Length > 0 ? args[0] :
                File.Exists(argsFileName) ? File.ReadAllText(argsFileName) : string.Empty;

            var appSettings = JsonConvert.DeserializeObject<AppSettings>(botSettingsStr);

            var kernel = new StandardKernel(new ConfigurationModule("log4net.config"));
            var connectionString = appSettings?.ConnectionString;

            Console.WriteLine($"Read {connectionString} connectionString");

            kernel.Rebind<IConnectionString>().ToConstant(new ConnectionString(connectionString));

            var botSettings = appSettings?.BotSettings ?? Array.Empty<BotSettings>();
            Console.WriteLine($"Read {botSettings.Length} bot settings");

            kernel.Unbind<IBotDaemon>();
            kernel.Bind<IBotDaemon>().To<EasyMemeBotDaemon>().Named("EasyMeme");
            kernel.Bind<IBotDaemon>().To<KonturPaymentsBotDaemon>().Named("KonturPayments");
            kernel.Bind<IBotDaemon>().To<ServerStorerBotDaemon>().Named("ServerStorer");
            kernel.Bind<IBotDaemon>().To<KCalMeterBotDaemon>().Named("KCalMeter");
            kernel.Bind<IBotDaemon>().To<LevDimovBotDaemon>().Named("LevDimover");
            kernel.Bind<IBotDaemon>().To<AlertBotDaemon>().Named("AlertBot");
            kernel.Bind<IBotDaemon>().To<CounterBotDaemon>().Named("CounterBot");
            kernel.Bind<IBotDaemon>().To<PlannyBotDaemon>().Named("Planny");

            using var cts = new CancellationTokenSource();

            foreach (var botSetting in botSettings)
            {
                var token = botSetting.Token;
             
                Console.WriteLine($"Start bot {token}");

                client = new TelegramBotClient(token);

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { } // receive all update types
                };

                if (botSetting.BotName == "Planny")
                {
                    kernel.Bind<PlanJobFactory>().ToConstant(new PlanJobFactory(client));
                    kernel.Bind<PlannyJobRunner>().ToSelf().InSingletonScope();
                }
                
                kernel.Bind<TelegramBotClient>().ToConstant(client).WhenAnyAncestorNamed(botSetting.BotName);
                kernel.Bind<CustomSettings>().ToConstant(new CustomSettings
                {
                    Raw = botSetting.CustomSettings ?? string.Empty
                }).WhenAnyAncestorNamed(botSetting.BotName);

                var botDaemon = kernel.Get<IBotDaemon>(botSetting.BotName);
                client.StartReceiving(
                    botDaemon.HandleUpdateAsync,
                    botDaemon.HandleErrorAsync,
                    receiverOptions,
                    cancellationToken: cts.Token);

                var me = client.GetMeAsync(cts.Token).GetAwaiter().GetResult();

                Console.WriteLine($"Start listening for @{me.Username}");
            }

            var jobRunner = kernel.Get<PlannyJobRunner>();
            jobRunner.Run().GetAwaiter().GetResult();
            
            Console.ReadLine();

            cts.Cancel();

            Console.WriteLine($"End Bots {DateTime.Now}");
        }
    }
}