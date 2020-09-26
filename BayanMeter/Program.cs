using System;
using System.IO;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

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
            try
            {
                client = new TelegramBotClient(token);
                client.OnMessage += BotOnMessageReceived;
                client.OnMessageEdited += BotOnMessageReceived;
                client.StartReceiving();
                Console.ReadLine();
            }
            finally
            {
                client.StopReceiving();
            }
        }

        private static void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message?.Type == MessageType.Text)
            {
                client.SendTextMessageAsync(message.Chat.Id, message.Text).GetAwaiter().GetResult();
            }
        }
    }
}