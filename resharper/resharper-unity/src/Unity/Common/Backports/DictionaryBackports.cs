#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Backports;

public static class DictionaryBackports
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        return dictionary.Remove(key, out value);
#else
        return dictionary.TryGetValue(key, out value) && dictionary.Remove(key); 
#endif
    }
}