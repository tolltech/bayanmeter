using System.IO;
using FluentAssertions;
using Ninject;
using NUnit.Framework;
using Tolltech.BayanMeterLib;

namespace Tolltech.BayanMeterTest
{
    public class ImageHashTest : TestBase
    {
        private IImageHasher imageHasher;

        protected override void Setup()
        {
            base.Setup();

            imageHasher = kernel.Get<IImageHasher>();
        }

        [Test]
        public void TestGetHash()
        {
            var inputFilePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Images", "test.jpg");

            File.Exists(inputFilePath).Should().BeTrue();

            var bytes = File.ReadAllBytes(inputFilePath);
            bytes.Should().NotBeEmpty();

            var hash = imageHasher.GetHash(bytes);
            hash.Should().NotBeEmpty();
        }
    }
}