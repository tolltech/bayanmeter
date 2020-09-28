using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Ninject;
using Telegram.Bot;
using Telegram.Bot.Types;
using Tolltech.BayanMeter.Psql;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;
using File = System.IO.File;

namespace Migration
{
    class Program
    {
        private static readonly string input = @"ChatExport_2020-09-28/result.json";

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
            var chat1 = client.GetChatAsync(new ChatId(-1001261621141)).GetAwaiter().GetResult();

            var inputText = File.ReadAllText(input);
            var chat = JsonConvert.DeserializeObject<ChatDto>(inputText);

            var total = chat.Messages.Length;
            var current = 0;
            var skip = 0;
            var errors = 0;
            var list = new List<Tolltech.BayanMeterLib.MessageDto>(chat.Messages.Length);
            var users = new Dictionary<int, ChatMember>();

            foreach (var message in chat.Messages)
            {
                ++current;
                Console.WriteLine($"{current}/{total}");
                if (string.IsNullOrWhiteSpace(message.PhotoPath))
                {
                    Console.WriteLine($"No Photo");
                    ++skip;
                    continue;
                }

                if (message.Type != "message")
                {
                    Console.WriteLine($"No Message");
                    ++skip;
                    continue;
                }

                var path = Path.Combine("../../../ChatExport_2020-09-28", message.PhotoPath);
                if (!File.Exists(path))
                {
                    Console.WriteLine($"No Photo exists");
                    ++errors;
                    ++skip;
                    continue;
                }

                var photo = File.ReadAllBytes(path);
                var now = DateTime.UtcNow;

                var user = users.TryGetValue(message.FromUserId, out var cashUser)
                    ? cashUser
                    : GetUser(client, message);

                users[message.FromUserId] = user;

                list.Add(new Tolltech.BayanMeterLib.MessageDto
                {
                    ChatId = -1001261621141,
                    CreateDate = now,
                    EditDate = null,
                    ForwardFromChatId = null,
                    ForwardFromChatName = null,
                    ForwardFromMessageId = 0,
                    ForwardFromUserId = null,
                    FromUserId = message.FromUserId,
                    FromUserName = user?.User?.Username ?? string.Empty,
                    ForwardFromUserName = null,
                    ImageBytes = photo,
                    IntId = message.Id,
                    MessageDate = message.Date,
                    StrId = $"-1001261621141_{message.Id}",
                    Text = string.Empty,
                    Timestamp = now.Ticks
                });
            }

            Console.WriteLine($"WriteTODb {list.Count} records");
            imageBayanService.SaveMessage(list.ToArray());

            Console.WriteLine($"WriteTODb SUccess");
        }

        private static ChatMember GetUser(TelegramBotClient client, MessageDto message)
        {
            try
            {
                return client.GetChatMemberAsync(-1001261621141, message.FromUserId).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
                return null;
            }
        }
    }
}