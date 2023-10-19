#nullable enable
using System.Runtime.CompilerServices;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Utils
{
    public static class CharPredicates
    {
        public struct IsWhitespacePredicate : IValueFunction<char, bool>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Invoke(char ch) => char.IsWhiteSpace(ch);
        }
    }
}