using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public static class ParserMessages
    {
        public const string IDS_TAG_DECLARATION = "Tag declaration";
        public const string IDS_TEXTURE_PROPERTY_VALUE_TEXTURE_DIMENSION = "Texture dimension";
        public const string IDS_ALPHA_TO_MASK_VALUE = "AlphaToMask value";
        public const string IDS_ALPHA_TEST_VALUE = "AlphaTest value";
        public const string IDS_ATTRIBUTE_PARAMETER_VALUE = "parameter value";
        public const string IDS_BIND_COMMAND = "Bind command";
        public const string IDS_BIND_VALUE = "Bind value";
        public const string IDS_BLEND_FACTOR = "Blend factor";
        public const string IDS_BLEND_OP_VALUE = "Blend op value";
        public const string IDS_BLEND_VALUE = "Blend value";
        public const string IDS_BLOCK_COMMAND = "Block command";
        public const string IDS_BLOCK_VALUE = "Block value";
        public const string IDS_CATEGORY_CONTENTS = "Category command";
        public const string IDS_CG_BLOCK = "CG block";
        public const string IDS_COMPARISON_FUNCTION_VALUE = "comparison function";
        public const string IDS_COLOR_MASK_VALUE = "ColorMask value";
        public const string IDS_COLOR_MATERIAL_VALUE = "color material value";
        public const string IDS_COLOR_VALUE = "color value";
        public const string IDS_CULL_VALUE = "cull value";
        public const string IDS_FOG_CONTENTS = "Fog command";
        public const string IDS_GRAB_PASS_CONTENTS = "GrabPass command";
        public const string IDS_INCLUDE_BLOCK = "include block";
        public const string IDS_LEGACY_LIGHTING_COMMAND = "lighting command";
        public const string IDS_LEGACY_RENDER_STATE_COMMAND = "render state command";
        public const string IDS_MATERIAL_CONTENTS = "Material command";
        public const string IDS_MODE_VALUE = "Mode value";
        public const string IDS_NUMERIC_VALUE = "numeric literal or referenced property";
        public const string IDS_ON_OFF_VALUE = "On or Off";
        public const string IDS_OPERATOR = "operator";
        public const string IDS_PASS_DEF = "pass definition";
        public const string IDS_PREPROCESSOR_DIRECTIVE = "preprocessor directive";
        public const string IDS_PROGRAM_BLOCK = "program block";
        public const string IDS_PROPERTY_TYPE = "property type";
        public const string IDS_REFERENCED_PROPERTY = "referenced property";
        public const string IDS_RENDER_STATE_COMMAND = "render state command";
        public const string IDS_REGULAR_PASS_CONTENTS = "Pass command";
        public const string IDS_SHADER_CONTENTS = "Shader contents";
        public const string IDS_SHADER_LAB_IDENTIFIER = "identifier";
        public const string IDS_SIMPLE_PROPERTY_TYPE = "property type";
        public const string IDS_STENCIL_CONTENTS = "Stencil command";
        public const string IDS_STENCIL_OPERATION = "stencil operation";
        public const string IDS_SUB_SHADER_CONTENTS = "SubShader contents";
        public const string IDS_ZTEST_VALUE = "ZTest value";

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