namespace Tolltech.BayanMeterLib
{
    public interface IImageHasher
    {
        string GetHash(byte[] jpegByteArray);
    }
}