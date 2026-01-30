using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Tolltech.AlertBot;
using Tolltech.BayanMeterLib.Psql;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.CoreLib;
using Tolltech.Counter;
using Tolltech.KCalMeter;
using Tolltech.KonturPaymentsLib;
using Tolltech.LevDimover;
using Tolltech.Planny;
using Tolltech.Runner;
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

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", Microsoft.Extensions.Logging.LogLevel.Warning);

var ignoreTypes = new HashSet<Type>
{
    typeof(IBotDaemon)
};
var bindings = IoCResolver.Resolve((@interface, implementation) => services.AddSingleton(@interface, implementation),
    ignoreTypes, "Tolltech");

foreach (var binding in bindings.OrderBy(x => x.Source.Namespace).ThenBy(x => x.Source.Name).ThenBy(x => x.Target.Name))
{
    log.Info($" -> Bind {binding.Source.Name} to {binding.Target.Name}");
}

log.Info($"Start Bots {DateTime.Now}");

var argsFileName = "args.txt";
var botSettingsStr = args.Length > 0 ? args[0] :
    File.Exists(argsFileName) ? File.ReadAllText(argsFileName) : string.Empty;

var appSettings = JsonConvert.DeserializeObject<AppSettings>(botSettingsStr)!;

var connectionString = appSettings.ConnectionString;

log.Info($"Read {connectionString} connectionString");

services.AddDbContextFactory<PlannyContext>((_, opt) => { opt.UseNpgsql(connectionString); });
services.AddDbContextFactory<MessageContext>((_, opt) => { opt.UseNpgsql(connectionString); });
services.AddDbContextFactory<CounterContext>((_, opt) => { opt.UseNpgsql(connectionString); });
services.AddDbContextFactory<FoodContext>((_, opt) => { opt.UseNpgsql(connectionString); });
services.AddDbContextFactory<FoodMessageContext>((_, opt) => { opt.UseNpgsql(connectionString); });
services.AddDbContextFactory<MoiraAlertContext>((_, opt) => { opt.UseNpgsql(connectionString); });

services.AddSingleton<MessageHandler>();
services.AddSingleton<CounterHandler>();
services.AddSingleton<FoodHandler>();
services.AddSingleton<FoodMessageHandler>();
services.AddSingleton<MoiraAlertHandler>();

var botSettings = appSettings.BotSettings;
log.Info($"Read {botSettings.Length} bot settings");

services.AddKeyedSingleton<IBotDaemon, PlannyBotDaemon>(PlannyBotDaemon.Key);
services.AddKeyedSingleton<IBotDaemon, EasyMemeBotDaemon>(EasyMemeBotDaemon.Key);
services.AddKeyedSingleton<IBotDaemon, KonturPaymentsBotDaemon>(KonturPaymentsBotDaemon.Key);
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
    try
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

        var me = await client.GetMe(cts.Token);

        log.Info($"Start listening for @{me.Username}");
    }
    catch (Exception e)
    {
        log.Fatal(e, $"Unable to start bot {botSetting.BotName}");
    }
}

log.Info($"Running host");
await host.RunAsync();
log.Info($"Run host");

Console.ReadLine();

cts.Cancel();

Console.WriteLine($"End Bots {DateTime.Now}");