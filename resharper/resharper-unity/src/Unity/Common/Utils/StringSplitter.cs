#nullable enable
using System.Collections.Generic;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Utils
{
    public static class StringSplitter
    {
        public static StringSplitter<CharPredicates.IsWhitespacePredicate> ByWhitespace(StringSlice input) => new(input, new CharPredicates.IsWhitespacePredicate());
    }
    
    public struct StringSplitter<TSeparatorPredicate> where TSeparatorPredicate : struct, IValueFunction<char, bool>
    {
        private readonly StringSlice myInput;
        private TSeparatorPredicate mySeparatorPredicate;
        private int myPosition;
        
        public StringSplitter(StringSlice input, TSeparatorPredicate separatorPredicate)
        {
            myInput = input;
            mySeparatorPredicate = separatorPredicate;
            myPosition = 0;
        }

        public bool TryGetNextSlice(out StringSlice nextSlice)
        {
            if (myPosition < myInput.Length)
            {
                var startPosition = -1;
                do
                {
                    var isSeparator = mySeparatorPredicate.Invoke(myInput[myPosition]);
                    if (isSeparator)
                    {
                        if (startPosition >= 0)
                        {
                            nextSlice = myInput.Substring(startPosition, myPosition - startPosition);
                            ++myPosition;
                            return true;
                        }
                    }
                    else if (startPosition < 0)
                        startPosition = myPosition;

                    ++myPosition;
                } while (myPosition < myInput.Length);

                if (startPosition >= 0)
                {
                    nextSlice = myInput.Substring(startPosition, myPosition - startPosition);
                    return true;
                }
            }

            nextSlice = StringSlice.Empty;
            return false;
        }
    }
}