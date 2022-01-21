#nullable enable

namespace JetBrains.ReSharper.Plugins.Json.Util
{
    internal class NamingUtil
    {
        public static bool IsIdentifier(string name)
        {
            if (name.Length == 0)
                return false;
            char[] charArray = name.ToCharArray();
            if (!char.IsLetter(charArray[0]) && charArray[0] != '_')
                return false;
            for (int index = 1; index < charArray.Length; ++index)
            {
                char c = charArray[index];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            return true;
        }
    }
}