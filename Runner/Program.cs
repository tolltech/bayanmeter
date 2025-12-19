using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Tolltech.AlertBot;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.CoreLib;
using Tolltech.Counter;
using Tolltech.KCalMeter;
using Tolltech.KonturPaymentsLib;
using Tolltech.LevDimover;
using Tolltech.Planny;
using Tolltech.PostgreEF.Integration;
using Tolltech.Runner;
using Tolltech.Runner.Psql;
using Tolltech.Storer;
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

log.Info($"Start Bots {DateTime.Now}");

var argsFileName = "args.txt";
var botSettingsStr = args.Length > 0 ? args[0] :
    File.Exists(argsFileName) ? File.ReadAllText(argsFileName) : string.Empty;

var appSettings = JsonConvert.DeserializeObject<AppSettings>(botSettingsStr)!;

var connectionString = appSettings.ConnectionString;

log.Info($"Read {connectionString} connectionString");

services.AddSingleton<IConnectionString>(new ConnectionString(connectionString));

var botSettings = appSettings.BotSettings;
log.Info($"Read {botSettings.Length} bot settings");

services.AddKeyedSingleton<IBotDaemon, PlannyBotDaemon>(PlannyBotDaemon.Key);
services.AddKeyedSingleton<IBotDaemon, EasyMemeBotDaemon>(EasyMemeBotDaemon.Key);
services.AddKeyedSingleton<IBotDaemon, KonturPaymentsBotDaemon>(KonturPaymentsBotDaemon.Key);
services.AddKeyedSingleton<IBotDaemon, ServerStorerBotDaemon>("ServerStorer");
services.AddKeyedSingleton<IBotDaemon, KCalMeterBotDaemon>("KCalMeter");
services.AddKeyedSingleton<IBotDaemon, LevDimovBotDaemon>("LevDimover");
services.AddKeyedSingleton<IBotDaemon, AlertBotDaemon>("AlertBot");
services.AddKeyedSingleton<IBotDaemon, CounterBotDaemon>("CounterBot");

using var cts = new CancellationTokenSource();

foreach (var botSetting in botSettings)
{
    var token = botSetting.Token;
 
    log.Info($"Configure bot {token}");

    var client = new TelegramBotClient(token);

    if (botSetting.BotName == PlannyBotDaemon.Key)
    {
        services.AddSingleton(new PlanJobFactory(client, log));
        services.AddSingleton<PlannyJobRunner>();
    }

    services.AddKeyedSingleton<ITelegramClient>(botSetting.BotName, new TelegramClient(client));
    services.AddKeyedSingleton(botSetting.BotName, client);
    services.AddKeyedSingleton(botSetting.BotName, new CustomSettings
    {
        Raw = botSetting.CustomSettings
    });
}

services.AddHostedService<PlanUpdater>();

log.Info("Building app");
using var host = builder.Build();
log.Info("Built app");

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

log.Info($"Running host");
await host.RunAsync();
log.Info($"Run host");

Console.ReadLine();

cts.Cancel();

Console.WriteLine($"End Bots {DateTime.Now}");