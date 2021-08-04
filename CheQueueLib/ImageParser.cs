using Tesseract;

namespace Tolltech.CheQueueLib
{
    public class ImageParser : IImageParser
    {
        public string Parse(byte[] bytes)
        {
            using var engine = new TesseractEngine("./tessdata", "rus", EngineMode.Default);
            using var img = Pix.LoadFromMemory(bytes);
            var page = engine.Process(img);

            return page.GetText();
            //using var iter = page.GetIterator();
            //iter.Next()
            //using var api = OcrApi.Create();

            //api.Init(new[] {Languages.Russian, Languages.English});

            //using var memoryStream = new MemoryStream(bytes);
            //using var bitMap = new Bitmap(memoryStream);
            //var plainText = api.GetTextFromImage(bitMap);
            //return plainText;
        }
    }
}