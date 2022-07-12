using System;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help
{
    internal static class StringEx
    {
        public static string ReplaceLast(this string input, char oldChar, char newChar)
        {
            var index = input.LastIndexOf(oldChar);

            if(index == -1)
                return input;

            var result = $"{input[..index]}{newChar}{input[(index+1)..]}";
            return result;
        }
    }
}