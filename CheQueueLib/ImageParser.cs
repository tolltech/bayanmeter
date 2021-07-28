using IronOcr;

namespace Tolltech.CheQueueLib
{
    public class ImageParser : IImageParser
    {
        public string Parse(byte[] bytes)
        {
            var Ocr = new IronTesseract();
            // Configure for speed
            Ocr.Configuration.BlackListCharacters = "~`$#^*_}{][|\\@";
            Ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.Auto;
            Ocr.Configuration.TesseractVersion = TesseractVersion.Tesseract5;
            Ocr.Configuration.EngineMode = TesseractEngineMode.LstmOnly;
            Ocr.Language = OcrLanguage.RussianFast;

            using var Input = new OcrInput(@"img\Potter.tiff");
            var Result = Ocr.Read(Input);
            return Result.Text;
        }
    }
}