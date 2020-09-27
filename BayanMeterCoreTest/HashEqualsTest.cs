using System.IO;
using Ninject;
using Tolltech.BayanMeterLib;
using Xunit;

namespace BayanMeterCoreTest
{
    public class HashEqualsTest : TestBase
    {
        private readonly IImageHasher imageHasher;

        public HashEqualsTest()
        {
            imageHasher = kernel.Get<IImageHasher>();
        }

        [Theory]
        [InlineData(true, "simple1", "simple2")]
        [InlineData(true, "complex1", "complex2")]
        [InlineData(false, "complex3", "complex2")]
        [InlineData(false, "complex1", "complex3")]
        public void TestHashEquals(bool expected, string left, string right)
        {
            var leftPath = Path.Combine(WorkDirecoryPath, "Images", $"{left}.jpg");
            var rightPath = Path.Combine(WorkDirecoryPath, "Images", $"{right}.jpg");

            Assert.True(File.Exists(leftPath));
            Assert.True(File.Exists(rightPath));

            var leftBytes = File.ReadAllBytes(leftPath);
            var rightBytes = File.ReadAllBytes(rightPath);
            Assert.NotEmpty(leftBytes);
            Assert.NotEmpty(rightBytes);

            var leftHash = imageHasher.GetHash(leftBytes);
            var rightHash = imageHasher.GetHash(rightBytes);
            Assert.Equal(expected, leftHash.HashEquals(rightHash));
        }
    }
}