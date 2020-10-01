namespace Tolltech.BayanMeterLib
{
    public static class HashEqualsExtensions
    {
        public static bool HashEquals(this string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return false;
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            var diffs = 0;
            for (var i = 0; i < left.Length; ++i)
            {
                if (left[i] != right[i])
                {
                    ++diffs;
                }
            }

            return diffs * 1.0 / left.Length < 0.03;
        }
    }
}