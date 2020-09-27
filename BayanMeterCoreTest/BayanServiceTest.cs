using Ninject;
using Tolltech.BayanMeterLib.TelegramClient;

namespace BayanMeterCoreTest
{
    public class BayanServiceTest : TestBase
    {
        private IImageBayanService imageBayanService;

        public BayanServiceTest()
        {
            imageBayanService = kernel.Get<IImageBayanService>();
        }
    }
}