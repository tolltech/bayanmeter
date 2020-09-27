using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Tolltech.BayanMeterLib
{
    public class ImageHasher : IImageHasher
    {
        public string GetHash(byte[] jpegByteArray)
        {
            //var image = new Bitmap(System.Drawing.Image.FromFile(inputPath))
            using (var image = Image.Load(jpegByteArray))
            {
                image.Mutate(x => x.Resize(8, 8).Grayscale());

                var pixelBytes = new List<byte>(image.Height * image.Width);

                for (var y = 0; y < image.Height; ++y)
                for (var x = 0; x < image.Width; ++x)
                {
                    pixelBytes.Add(image[x, y].R);
                }

                var avg = pixelBytes.Average(x => x);

                return string.Join(string.Empty, pixelBytes.Select(x => x > avg ? 1 : 0));
            }
        }
    }
}