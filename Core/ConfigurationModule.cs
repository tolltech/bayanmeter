using System;
using System.IO;
using log4net.Config;
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
                    throw new Exception($"Logger configuration file {fileInfo.FullName} not found");
                XmlConfigurator.Configure(fileInfo);
            }

            IoCResolver.Resolve((@interface, implementation) => this.Bind(@interface).To(implementation), null,
                "Tolltech");
        }
    }
}