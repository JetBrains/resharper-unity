using JetBrains.Util;
// ReSharper disable InconsistentNaming

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing
{
    public static class ParserMessages
    {
        public const string IDS_STATEMENT = "statement";
        
        public const string IDS_FLOW_CONTROL_STATEMENT = "flow control statement";
        
        public const string IDS_ASSIGNMENT_OPERATOR = "assignment operator";
        
        public const string IDS_POSTFIX_OPERATOR = "postfix operator";
        
        public const string IDS_PREFIX_OPERATOR = "prefix operator";
        
        public const string IDS_RUNTIME_VALUE = "runtime value";
        
        public const string IDS_PRIMARY_EXPRESSION = "value";
        
        public const string IDS_BINARY_OPERATOR = "binary operator";
        
        public const string IDS_IDENTIFIER = "identifier";
        
        public const string IDS_CONSTANT_VALUE = "constant value";
        
        public const string IDS_DECLARATION = "field, function or structure declaration";

        public const string IDS_BUILT_IN_TYPE = "built-in type";
        
        public const string IDS_TYPE_NAME = "type name";
        
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