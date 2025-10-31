using System;
using System.Collections.Generic;
using System.Linq;

namespace Tolltech.CoreLib.Helpers
{
    public static class StringExtensions
    {
        public static string GetArgument(this string src, int argumentNumber)
        {
            return src.GetArguments().ElementAt(argumentNumber);
        }

        public static IEnumerable<string> GetArguments(this string src)
        {
            return src.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Skip(1);
        }

        public static string JoinToString<T>(this IEnumerable<T> src, string separator = ",")
        {
            return string.Join(separator, src);
        }
    }
}