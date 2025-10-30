using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Tolltech.BotRunner;
using Tolltech.BotRunner.Psql;
using Tolltech.Core;
using Tolltech.CoreLib;
using Tolltech.Planny;
using Tolltech.PostgreEF.Integration;
using Tolltech.TelegramCore;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.Logging.File;
using Vostok.Logging.File.Configuration;

// Create host builder
var builder = Host.CreateApplicationBuilder(args);

// Configure logging
var fileLog = new FileLog(new FileLogSettings
{
    FilePath = "logs/vostok.log", // Path to your log file
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

var ignoreTypes = new HashSet<Type>
{
    typeof(IConnectionString),
    typeof(IBotDaemon)
};
IoCResolver.Resolve((@interface, implementation) => services.AddSingleton(@interface, implementation), ignoreTypes, "Tolltech");

Console.WriteLine($"Start Bots {DateTime.Now}");

var argsFileName = "args.txt";
var botSettingsStr = args.Length > 0 ? args[0] :
    File.Exists(argsFileName) ? File.ReadAllText(argsFileName) : string.Empty;

var appSettings = JsonConvert.DeserializeObject<AppSettings>(botSettingsStr)!;

var connectionString = appSettings.ConnectionString;

Console.WriteLine($"Read {connectionString} connectionString");

services.AddSingleton<IConnectionString>(new ConnectionString(connectionString));

var botSettings = appSettings.BotSettings;
Console.WriteLine($"Read {botSettings.Length} bot settings");

services.AddKeyedSingleton<IBotDaemon, PlannyBotDaemon>(PlannyBotDaemon.Key);

using var cts = new CancellationTokenSource();

foreach (var botSetting in botSettings)
{
    var token = botSetting.Token;
 
    Console.WriteLine($"Start bot {token}");

    var client = new TelegramBotClient(token);

    if (botSetting.BotName == PlannyBotDaemon.Key)
    {
        services.AddSingleton(new PlanJobFactory(client, log));
        services.AddSingleton<PlannyJobRunner>();
    }

    services.AddKeyedSingleton(botSetting.BotName, client);
    services.AddKeyedSingleton(botSetting.BotName, new CustomSettings
    {
        Raw = botSetting.CustomSettings
    });
}

using var host = builder.Build();

var jobRunner = host.Services.GetRequiredService<PlannyJobRunner>();
await jobRunner.Run();

foreach (var botSetting in botSettings)
{
    var client = host.Services.GetKeyedService<TelegramBotClient>(botSetting.BotName);
    var botDaemon = host.Services.GetKeyedService<IBotDaemon>(botSetting.BotName);
    if (botDaemon == null)
    {
        log.Error($"Error getting @{botSetting.BotName}");    
        continue;
    }

    if (client == null)
    {
        log.Error($"Error getting client @{botSetting.BotName}");    
        continue;
    }
    
    var receiverOptions = new ReceiverOptions
    {
        AllowedUpdates = { } // receive all update types
    };
    
    client.StartReceiving(
        botDaemon.HandleUpdateAsync,
        botDaemon.HandleErrorAsync,
        receiverOptions,
        cancellationToken: cts.Token);

    var me = client.GetMeAsync(cts.Token).GetAwaiter().GetResult();

    log.Info($"Start listening for @{me.Username}");
}

// Run the application
await host.RunAsync();

Console.ReadLine();

cts.Cancel();

Console.WriteLine($"End Bots {DateTime.Now}");