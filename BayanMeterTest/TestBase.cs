using Ninject;
using NUnit.Framework;
using Tolltech.Core;

namespace Tolltech.BayanMeterTest
{
    [TestFixture]
    public abstract class TestBase
    {
        protected StandardKernel kernel;

        [SetUp]
        protected virtual void Setup()
        {
            kernel = new StandardKernel(new ConfigurationModule());
        }

        [TearDown]
        protected virtual void TearDown()
        {

        }
    }
}