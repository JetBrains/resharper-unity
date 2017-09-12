using JetBrains.Util;
// ReSharper disable InconsistentNaming

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing
{
    public static class ParserMessages
    {
        public static string IDS_EXPRESSION => "expression";
        
        public static string IDS_GLOBAL_VARIABLE_DECLARATION => "global variable";
        
        public static string IDS_CONSTANT_VALUE => "constant value";
        
        public static string IDS_DECLARATION => "field, function or structure declaration";
        
        public static string IDS_FIELD_OR_FUNCTION_DECLARATION => "field or function declaration";
        
        public static string IDS_FIELD_DECLARATION => "field declaration";

        public static string IDS_BUILT_IN_TYPE => "built-in type";
        
        public static string IDS_TYPE_NAME => "type name";
        
        public static string IDS_STRUCT_DECLARATION => "struct declaration";
        
        public static string IDS_FUNCTION_RETURN_TYPE => "function return type";
        
        public static string IDS_FUNCTION_DECLARATION => "function declaration";
        
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