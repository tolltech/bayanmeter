using System;
using System.IO;
using System.Linq;
using Ninject;
using Telegram.Bot;
using Tolltech.BayanMeterLib.TelegramClient;

namespace Tolltech.BayanMeter
{
    class Program
    {
        private static TelegramBotClient client;

        static void Main(string[] args)
        {
            var argsFileName = "args.txt";
            var token = args.FirstOrDefault()
                        ?? (File.Exists(argsFileName)
                            ? File.ReadAllLines(argsFileName).FirstOrDefault()
                            : string.Empty);

            var kernel = new StandardKernel(new ConfigurationModule("log4net.config"));
            client = new TelegramBotClient(token);
            kernel.Bind<TelegramBotClient>().ToConstant(client);

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
            }
        }
    }
}