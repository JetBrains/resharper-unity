using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public static class ParserMessages
    {
        public const string IDS_ATTRIBUTE_PARAMETER_VALUE = "parameter value";
        public const string IDS_COMPARISON_FUNCTION_VALUE = "comparison function";
        public const string IDS_COLOR_MATERIAL_VALUE = "color material value";
        public const string IDS_COLOR_VALUE = "color value";
        public const string IDS_CULL_COMMAND = "cull command";
        public const string IDS_CULL_VALUE = "cull value";
        public const string IDS_LEGACY_LIGHTING_COMMAND = "lighting command";
        public const string IDS_MATERIAL_VALUE_COMMAND = "material command";
        public const string IDS_ON_OFF_VALUE = "On or Off";
        public const string IDS_PASS_DEF = "pass definition";
        public const string IDS_PROPERTY_TYPE = "property type";
        public const string IDS_RENDER_STATE_COMMAND = "render state command";
        public const string IDS_SHADER_LAB_IDENTIFIER = "identifier";
        public const string IDS_SHININESS_VALUE = "shininess value";
        public const string IDS_SIMPLE_PROPERTY_TYPE = "property type";
        public const string IDS_STENCIL_OPERATION = "stencil operation";
        public const string IDS_STENCIL_VALUE_COMMAND = "stencil command";

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