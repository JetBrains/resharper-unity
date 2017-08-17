using JetBrains.Util;
// ReSharper disable InconsistentNaming

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing
{
    public static class ParserMessages
    {
        public static string IDS_CG_FIELD_DECLARATION => "field declaration";

        public static string IDS_CG_BUILT_IN_TYPE => "built-in type";
        
        public static string IDS_CG_TYPE_NAME => "type name";
        
        public static string IDS_CG_STRUCT_DECLARATION => "struct declaration";
        
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