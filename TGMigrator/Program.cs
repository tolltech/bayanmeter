// See https://aka.ms/new-console-template for more information

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TGMigrator;
using Tolltech.BayanMeterLib.Psql;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.Logging.File;
using Vostok.Logging.File.Configuration;

// Create host builder
var builder = Host.CreateApplicationBuilder(args);

// Configure logging
var fileLog = new FileLog(new FileLogSettings
{
    FilePath = "logs/vostok3.log", // Path to your log file
    RollingStrategy = new RollingStrategyOptions // Optional: configure log file rolling
    {
        Type = RollingStrategyType.BySize,
        MaxSize = 100 * 1024 * 1024, // 100 MB
        MaxFiles = 5 // Keep up to 5 log files
    }
});
var consoleLog = new ConsoleLog();
var log = new CompositeLog(fileLog, consoleLog);


var services = builder.Services;
services.AddSingleton<ILog>(log);

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", Microsoft.Extensions.Logging.LogLevel.Warning);

Console.WriteLine("Hello, World!");

var messages = JsonConvert.DeserializeObject<HistoryMessage[]>(await File.ReadAllTextAsync("./input.txt"));
var connectionString = await File.ReadAllTextAsync("ConnectionString.txt");

services.AddDbContextFactory<MessageContext>((_, opt) => { opt.UseNpgsql(connectionString); });
services.AddSingleton<MessageHandler>();

log.Info("Building app");
using var host = builder.Build();
log.Info("Built app");

var messageHandler = host.Services.GetRequiredService<MessageHandler>();

var total = messages.Length;
var skipped = new List<HistoryMessage>();
var processed = new List<HistoryMessage>();
var errors = new List<(HistoryMessage, string)>();
var cnt = 0;

foreach (var message in messages)
{
    try
    {
        log.Info($"Processing {message.MessageId} {message.Type} {++cnt}/{total} messages. Skipped {skipped.Count} Errors {errors.Count}");

        if (message.MessageId == null)
        {
            throw new ArgumentException("MessageId is required");
        }

        if (message.Type == null)
        {
            throw new ArgumentException("MessageId is required");
        }

        if (message.Type == "service")
        {
            skipped.Add(message);
            continue;
        }
        
        if (message.Type != "message")
        {
            throw new ArgumentException("Message is not message");
        }

        if (message.Reactions.Any(x => x.Count == null || x.Type == null))
        {
            throw new ArgumentException("Reactions has null fields");
        }

        if (message.Reactions.Length == 0)
        {
            skipped.Add(message);
            continue;
        }

        var strId = MessageHelper.GetStrId(-1001261621141, message.MessageId.Value);
        var dbMessage = await messageHandler.Find(strId);
        if (dbMessage == null)
        {
            skipped.Add(message);
            continue;
        }

        var reactions = message.GetReactions().ToArray();

        log.Info($"Got {reactions.Length} reactions of {strId}");

        await messageHandler.UpdateReactions(strId, reactions, message.Reactions.Sum(x => x.Count!.Value));

        processed.Add(message);
    }
    catch (Exception e)
    {
        log.Error(e, $"{message.MessageId}");
        errors.Add((message, e.Message));
        continue;
    }
}

File.WriteAllText("processed3.json", JsonConvert.SerializeObject(processed, Formatting.Indented));
File.WriteAllText("skipped3.json", JsonConvert.SerializeObject(skipped, Formatting.Indented));
File.WriteAllText("errors3.json", JsonConvert.SerializeObject(errors, Formatting.Indented));

Console.ReadLine();