using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Ninject.Modules;

namespace Tolltech.Core
{
    public class ConfigurationModule : NinjectModule
    {
        private readonly string log4NetFileName;

        public ConfigurationModule(string log4NetFileName = null)
        {
            this.log4NetFileName = log4NetFileName;
        }

        public override void Load()
        {
            if (!string.IsNullOrWhiteSpace(log4NetFileName))
            {
                var fileInfo = new FileInfo(log4NetFileName);
                if (!fileInfo.Exists)
                {
                    Console.WriteLine($"Logger configuration file {fileInfo.FullName} not found. Use default");
                    Logger.Setup();
                }

                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo(log4NetFileName));

                Console.WriteLine($"Logger was configured");
            }

            IoCResolver.Resolve((@interface, implementation) => this.Bind(@interface).To(implementation), null,
                "Tolltech");
        }

        public class Logger
        {
            public static void Setup()
            {
                var hierarchy = (Hierarchy)LogManager.GetRepository();

                var patternLayout = new PatternLayout();
                patternLayout.ConversionPattern = "%date %-6timestamp %-5level %message%newline";
                patternLayout.ActivateOptions();

                var roller = new RollingFileAppender();
                roller.AppendToFile = true;
                roller.File = @"logs/log";
                roller.Layout = patternLayout;
                roller.MaximumFileSize = "5000KB";
                roller.RollingStyle = RollingFileAppender.RollingMode.Date;
                roller.StaticLogFileName = false;            
                roller.DatePattern = "yyyy.MM.dd";            
                roller.ActivateOptions();
                hierarchy.Root.AddAppender(roller);

                hierarchy.Root.Level = Level.Debug;
                hierarchy.Configured = true;
            }
        }
    }
}