using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Tolltech.BayanMeterLib.Helpers;

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
            var photoSize = message?.Photo?.FirstOrDefault();

            if (photoSize == null)
            {
                return;
            }

            byte[] bytes;
            Telegram.Bot.Types.File file;
            using (var stream = new MemoryStream(photoSize.FileSize))
            {
                file = client.GetFileAsync(photoSize.FileId).GetAwaiter().GetResult();
                client.DownloadFileAsync(file.FilePath, stream).GetAwaiter().GetResult();
                stream.Seek(0, SeekOrigin.Begin);

                bytes = stream.ReadToByteArray();
            }

            File.WriteAllBytes("test.jpg", bytes);

            // if (string.IsNullOrEmpty(photo))
            // {
            //     return;
            // }

            if (message?.Type == MessageType.Photo)
            {
                client.SendTextMessageAsync(message.Chat.Id, $"{message.Text} {photoSize.Height} {photoSize.Width} {bytes.Length}").GetAwaiter().GetResult();
            }
        }
    }
}