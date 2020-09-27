using SixLabors.ImageSharp;

namespace Tolltech.BayanMeterLib
{
    public class ImageHasher : IImageHasher
    {
        public string GetHash(byte[] jpegByteArray)
        {
            //var image = new Bitmap(System.Drawing.Image.FromFile(inputPath))
            using (var image = Image.Load(jpegByteArray))
            {
                return "";
            }
        }
    }
}