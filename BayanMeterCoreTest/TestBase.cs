using Ninject;
using Tolltech.Core;

namespace BayanMeterCoreTest
{
    public abstract class TestBase
    {
        protected StandardKernel kernel;

        protected TestBase()
        {
            kernel = new StandardKernel(new ConfigurationModule());
        }
    }}