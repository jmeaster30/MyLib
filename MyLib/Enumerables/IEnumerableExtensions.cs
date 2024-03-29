using System.Security.Cryptography;

namespace MyLib.Enumerables;

public static class EnumerableExtensions {
    public static IEnumerable<T> AddFill<T>(this IEnumerable<T> a, int from, int to, Func<int, T> function)
    {
        var results = new List<T>(a);
        for (var i = from; i < to; i++)
        {
            results.Add(function(i));
        }
        return results;
    }
    
    public static IEnumerable<T> PadRight<T>(this IEnumerable<T> a, int amount, T value)
    {
        var result = new List<T>();
        for (var i = 0; i < amount; i++)
        {
            result.Add(i < a.Count() ? a.ElementAt(i) : value);
        }
        return result;
    }
    
    public static IEnumerable<T> PadLeft<T>(this IEnumerable<T> a, int amount, T value)
    {
        var result = new List<T>();
        for (var i = 0; i < amount; i++)
        {
            if (i < a.Count())
            {
                result.Add(a.ElementAt(i));
            }
            else
            {
                result = result.Prepend(value).ToList();
            }

        }
        return result;
    }

    // TODO I feel like this API could be better :/
    public static IEnumerable<(T, int)> RunGroupBy<T, U>(this IEnumerable<T> input, Func<T, U> selector, int maxChunkSize)
    {
        var result = new List<(T, int)>();
        (T, int) current = (default!, 0);
        foreach (var i in input)
        {
            if (current.Item2 != 0 && EqualityComparer<U>.Default.Equals(selector(current.Item1), selector(i)) && current.Item2 < maxChunkSize - 1)
            {
                current = (current.Item1, current.Item2 + 1);
            }
            else
            {
                if (current.Item2 > 0) result.Add(current);
                current = (i, 1);
            }
        }
        if (current.Item2 > 0) result.Add(current);
        return result;
    }

    public static IEnumerable<T> Repeat<T>(this T value, int amount)
    {
        var results = new List<T>();
        while (results.Count < amount)
        {
            results.Add(value);
        }

        return results;
    }

    public static bool AnyInRange<T>(this IEnumerable<T> enumerable, int from, int to, Func<T, bool> predicate)
    {
        var index = 0;
        foreach (var value in enumerable)
        {
            if (index >= from && index < to && predicate(value))
            {
                return true;
            }

            index += 1;
            if (index >= to) break;
        }

        return false;
    }
    
    public static bool AllInRange<T>(this IEnumerable<T> enumerable, int from, int to, Func<T, bool> predicate)
    {
        var index = 0;
        foreach (var value in enumerable)
        {
            if (index >= from && index < to && !predicate(value))
            {
                return false;
            }

            index += 1;
            if (index >= to) break;
        }

        return true;
    }
    
    public static (int, T?, T?) FirstDifference<T>(this IEnumerable<T> left, IEnumerable<T> right) where T : struct
    {
        var index = 0;
        T? lResult = null;
        T? rResult = null;
        for (; index < left.Count(); index++)
        {
            lResult = left.ElementAt(index);
            rResult = index < right.Count() ? right.ElementAt(index) : null;
            if (rResult == null) break;
            if (!EqualityComparer<T>.Default.Equals(lResult.Value, rResult.Value)) break;
        }

        if (index == left.Count() && index == right.Count())
        {
            return (-1, null, null);
        }
        
        return (index, lResult, rResult);
    }

    private static ulong Mix(ulong v)
    {
        var result = v;
        result ^= result >> 23;
        result *= 0x2127599bf4325c37;
        result ^= result >> 47;
        return result;
    }
    
    public static ulong Hash(this IEnumerable<byte> input)
    {
        // modified version of the fast hash algorithm from https://github.com/ztanml/fast-hash but converted to C# and also changed a little bit
        const ulong m = 0x880355f21e6d1965;
        const ulong seed = 0; // not sure how the seed works??
        var h = seed ^ ((ulong)input.LongCount() * m);
        return input.Chunk(8)
            .Select(x => x.Aggregate((ulong)0, (result, b) => (result << 8) | b))
            .ToList()
            .Aggregate(h, (hp, v) => (hp ^ Mix(v)) * m);
    }

    public static string Md5Hash(this IEnumerable<byte> input)
    {
        using var md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(input.ToArray()));
    }

    public static string Join(this IEnumerable<string> input, string sep)
    {
        var s = "";
        for (var i = 0; i < input.Count(); i++)
        {
            s += input.ElementAt(i);
            if (i < input.Count() - 1)
            {
                s += sep;
            }
        }
        return s;
    }
    
    public static string Join(this IEnumerable<string> input, char sep)
    {
        return input.Join(sep.ToString());
    }
    
    public static string Join(this IEnumerable<char> input, string sep)
    {
        var s = "";
        for (var i = 0; i < input.Count(); i++)
        {
            s += input.ElementAt(i);
            if (i < input.Count() - 1)
            {
                s += sep;
            }
        }
        return s;
    }
    
    public static string Join(this IEnumerable<char> input, char sep)
    {
        return input.Join(sep.ToString());
    }

    public static byte ToByte(this IEnumerable<byte> input)
    {
        if (input.Count() != 1)
            throw new ArgumentOutOfRangeException($"Byte array has too few or too many values. Expected 1 but got {input.Count()}",
                nameof(input));
        return input.First();
    }
    
    public static ushort ToU16(this IEnumerable<byte> input)
    {
        if (input.Count() != 2)
            throw new ArgumentOutOfRangeException($"Byte array has too few or too many values. Expected 2 but got {input.Count()}",
                nameof(input));
        return BitConverter.ToUInt16(BitConverter.IsLittleEndian ? input.Reverse().ToArray() : input.ToArray());
    }
    
    public static uint ToU32(this IEnumerable<byte> input)
    {
        if (input.Count() != 4)
            throw new ArgumentOutOfRangeException($"Byte array has too few or too many values. Expected 4 but got {input.Count()}",
                nameof(input));
        return BitConverter.ToUInt32(BitConverter.IsLittleEndian ? input.Reverse().ToArray() : input.ToArray());
    }

    public static string ToHexString(this IEnumerable<byte> input)
    {
        return input.Select(b => b.ToString("X2")).Join(' ');
    }
}