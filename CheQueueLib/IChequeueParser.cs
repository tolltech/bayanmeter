using JetBrains.Annotations;

namespace Tolltech.CheQueueLib
{
    public interface IImageParser
    {
        [NotNull] string Parse([NotNull] byte[] bytes);
    }
}