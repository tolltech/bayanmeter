using System.IO;
using FluentAssertions;
using Ninject;
using NUnit.Framework;
using Tolltech.CheQueueLib;

namespace Tolltech.CheQueueTest
{
    public class ImageParserTest : TestBase
    {
        private IImageParser imageParser;

        public override void Setup()
        {
            base.Setup();

            imageParser = kernel.Get<IImageParser>();
        }

        [Test]
        public void TestDummy()
        {
            var text = imageParser.Parse(File.ReadAllBytes(Path.Combine(WorkDirectoryPath, "Img", "cheque1.jpg")));
            text.Should().NotBeEmpty();
        }
    }
}