using System.IO;
using System.Reflection;
using Ninject;
using Tolltech.Core;

namespace BayanMeterCoreTest
{
    public abstract class TestBase
    {
        protected StandardKernel kernel;

        protected string WorkDirecoryPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        protected TestBase()
        {
            kernel = new StandardKernel(new ConfigurationModule());
        }
    }}