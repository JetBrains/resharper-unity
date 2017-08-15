using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing
{
    public static class ParserMessages
    {
        public static string GetString(string id) => id;
        
        public static string GetExpectedMessage(string tokenRepr)
        {
            return string.Format(GetString("{0} expected"), tokenRepr).Capitalize(); // why the GetString?
        }
    }
}