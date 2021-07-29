using System.Drawing;
using System.IO;
using Patagames.Ocr;
using Patagames.Ocr.Enums;

namespace Tolltech.CheQueueLib
{
    public class ImageParser : IImageParser
    {
        public string Parse(byte[] bytes)
        {
            using var api = OcrApi.Create();

            api.Init(new[] {Languages.Russian, Languages.English});

            using var memoryStream = new MemoryStream(bytes);
            using var bitMap = new Bitmap(memoryStream);
            var plainText = api.GetTextFromImage(bitMap);
            return plainText;
        }
    }
}