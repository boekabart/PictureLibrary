using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace SmartMirror
{
    public static class Helpers
    {
        public static Exception AddPath(this IOException ioe, string path)
        {
            return new EnrichedIOException(String.Format("{0} ({1})", ioe.Message, path), ioe);
        }

        public static Exception AddPaths(this IOException ioe, string path, string path2)
        {
            return new EnrichedIOException(String.Format("{0} ({1}, {2})", ioe.Message, path, path2), ioe);
        }

        public static double WeighedAverage<TSource>(this IEnumerable<TSource> source,
            Func<TSource, double> valueSelector,
            Func<TSource, double> weightSelector)
        {
            return source.Sum(item => valueSelector(item) * weightSelector(item)) / source.Sum(weightSelector);
        }

        public static string JoinStrings(this IEnumerable<string> enumeration, string separator)
        {
            return String.Join(separator, enumeration.ToArray());
        }

        public static TSource RoundRobinOrDefault<TSource>(this IEnumerable<TSource> enumeration, ref int token)
        {
            var try1 = enumeration.Skip(token).FirstOrDefault();
            token++;
            if (try1 != null)
                return try1;
            token = 1;
            return enumeration.FirstOrDefault();
        }

        public static T ParseEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static string FirstOrEmpty(this IEnumerable<string> enumeration)
        {
            return enumeration.FirstOrDefault() ?? String.Empty;
        }

        public static TValue TryGetElseCreateAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> initialize)
        {
            TValue templateEx;
            if (dictionary.TryGetValue(key, out templateEx))
                return templateEx;

            templateEx = initialize();
            dictionary.TryAdd(key, templateEx);
            return templateEx;
        }
    }
}