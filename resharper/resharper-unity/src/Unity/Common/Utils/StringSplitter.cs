#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Utils
{
    public readonly struct CharPredicateSkipper<T>(T predicate) : IValueFunction<StringSlice, int, int>
        where T : IValueFunction<char, bool>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Invoke(StringSlice slice, int position)
        {
            do
            {
                if (!predicate.Invoke(slice[position]))
                    return position;
                ++position;
            } while (position < slice.Length);

            return position;
        }
    }
    
    public static class StringSplitter
    {
        public static StringSplitter<CharPredicateSkipper<CharPredicates.IsWhitespacePredicate>> ByWhitespace(StringSlice input) => ByPredicate(input, new CharPredicates.IsWhitespacePredicate());

        public static StringSplitter<CharPredicateSkipper<T>> ByPredicate<T>(StringSlice input, T predicate) where T : IValueFunction<char, bool> => new(input, new CharPredicateSkipper<T>(predicate));
    }
    
    public struct StringSplitter<TSeparatorSkipper> where TSeparatorSkipper : struct, IValueFunction<StringSlice, int, int>
    {
        private readonly StringSlice myInput;
        private TSeparatorSkipper mySeparatorSkipper;
        private int myPosition;
        
        public StringSplitter(StringSlice input, TSeparatorSkipper separatorSkipper)
        {
            myInput = input;
            mySeparatorSkipper = separatorSkipper;
            myPosition = myInput.Length > 0 ? mySeparatorSkipper.Invoke(myInput, 0) : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNextSlice(out StringSlice nextSlice) => TryGetNextSlice(out nextSlice, out _);        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetNextSliceAsString([MaybeNullWhen(false)] out string next) => (next = TryGetNextSlice(out var nextSlice, out _) ? nextSlice.ToString() : null) != null;

        public bool TryGetNextSlice(out StringSlice nextSlice, out int startPosition)
        {
            var length = myInput.Length;
            startPosition = myPosition;
            if (startPosition >= length)
            {
                nextSlice = StringSlice.Empty;
                return false;
            }

            var endPosition = ++myPosition;
            while (endPosition < length)
            {
                myPosition = mySeparatorSkipper.Invoke(myInput, endPosition);
                if (myPosition != endPosition)
                    break;

                endPosition = ++myPosition;
            }

            nextSlice = myInput.Substring(startPosition, endPosition - startPosition);
            return true;
        }
    }
}