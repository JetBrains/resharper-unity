using JetBrains.Util;
// ReSharper disable InconsistentNaming

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing
{
    public static class ParserMessages
    {
        public const string IDS_JSON_NEW_VALUE = "json value";
        public const string IDS_JSON_NEW_LITERAL_EXPRESSION= "literal";
        public static string GetString(string id) => id;
        
        public static string GetUnexpectedTokenMessage() => "Unexpected token";
        
        public static string GetExpectedMessage(string tokenRepr)
        {
            return string.Format(GetString("{0} expected"), tokenRepr).Capitalize(); // why the GetString?
        }
        
        public static string GetExpectedMessage(string firstExpectedSymbol, string secondExpectedSymbol)
        {
            return string.Format(GetString("{0} or {1} expected"), firstExpectedSymbol, secondExpectedSymbol).Capitalize();
        }
    }
}