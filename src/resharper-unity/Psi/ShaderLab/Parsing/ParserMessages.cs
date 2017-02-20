using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public static class ParserMessages
    {
        public const string IDS_SHADER_LAB_IDENTIFIER = "identifier";
        public const string IDS_PROPERTY_TYPE = "property type";
        public const string IDS_SIMPLE_PROPERTY_TYPE = "property type";
        public const string IDS_PROPERTY_VALUE = "property value";
        public const string IDS_ATTRIBUTE_PARAMETER_VALUE = "parameter value";
        public const string IDS_FALLBACK_VALUE = "fallback value";

        public static string GetString(string id) => id;

        public static string GetUnexpectedTokenMessage() => "Unexpected token";

        public static string GetExpectedMessage(string expectedSymbol)
        {
            return string.Format(GetString("{0} expected"), expectedSymbol).Capitalize();
        }

        public static string GetExpectedMessage(string firstExpectedSymbol, string secondExpectedSymbol)
        {
            return string.Format(GetString("{0} or {1} expected"), firstExpectedSymbol, secondExpectedSymbol).Capitalize();
        }
    }
}