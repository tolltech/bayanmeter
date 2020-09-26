using SixLabors.ImageSharp;

namespace Tolltech.BayanMeterLib
{
    public class ImageHasher : IImageHasher
    {
        public string GetHash(byte[] jpegByteArray)
        {
            using (var image = Image.Load(jpegByteArray))
            {
                return "";
            }
        }
    }
}