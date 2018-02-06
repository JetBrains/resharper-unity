﻿using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public static class ParserMessages
    {
        public const string IDS_ALPHA_TEST_VALUE = "AlphaTest value";
        public const string IDS_ATTRIBUTE_PARAMETER_VALUE = "parameter value";
        public const string IDS_BIND_COMMAND = "Bind command";
        public const string IDS_BLEND_FACTOR = "Blend factor";
        public const string IDS_BLEND_VALUE = "Blend value";
        public const string IDS_BLOCK_COMMAND = "Block command";
        public const string IDS_BLEND_OP_VALUE_AUX = "BlendOp value";
        public const string IDS_BLOCK_VALUE = "Block value";
        public const string IDS_BOOL_LITERAL = "bool";
        public const string IDS_BOOL_VALUE = "bool";
        public const string IDS_BRIGHTNESS_MODIFIER = "Brightness modifier";
        public const string IDS_COMPARISON_FUNCTION_VALUE = "comparison function";
        public const string IDS_COLOR_MASK_RGBA_FLAGS = "RGBA flags";
        public const string IDS_COLOR_MATERIAL_VALUE = "color material value";
        public const string IDS_COLOR_VALUE = "color value";
        public const string IDS_CULL_ORIENTATION = "cull orientation";
        public const string IDS_CULL_VALUE = "cull value";
        public const string IDS_FALSE_LITERAL = "false/off";
        public const string IDS_FOG_CONTENTS = "Fog command";
        public const string IDS_GRAB_PASS_CONTENTS = "GrabPass command";
        public const string IDS_INCLUDE_BLOCK = "include block";
        public const string IDS_INVALID_VARIABLE_REFERENCE_PARAMETERS = "invalid parameters";
        public const string IDS_LEGACY_LIGHTING_COMMAND = "lighting command";
        public const string IDS_LEGACY_OPERATOR = "Legacy operator";
        public const string IDS_LEGACY_RENDER_STATE_COMMAND = "render state command";
        public const string IDS_MATERIAL_CONTENTS = "Material command";
        public const string IDS_MODE_VALUE = "Mode value";
        public const string IDS_NUMERIC_VALUE = "numeric literal or referenced property";
        public const string IDS_OPERATOR = "operator";
        public const string IDS_PASS_DEF = "pass definition";
        public const string IDS_PREPROCESSOR_DIRECTIVE = "preprocessor directive";
        public const string IDS_PROGRAM_BLOCK = "program block";
        public const string IDS_PROPERTY_TYPE = "property type";
        public const string IDS_RENDER_STATE_COMMAND = "render state command";
        public const string IDS_SHADER_BLOCK = "shader block";
        public const string IDS_SHADER_LAB_IDENTIFIER = "identifier";
        public const string IDS_SIMPLE_PROPERTY_TYPE = "property type";
        public const string IDS_STATE_COMMAND = "state command";
        public const string IDS_STENCIL_CONTENTS = "Stencil command";
        public const string IDS_STENCIL_OPERATION = "stencil operation";
        public const string IDS_TAG_DECLARATION = "tag";
        public const string IDS_TEX_ENV_PROPERTY = "TexEnv property";
        public const string IDS_TEX_GEN_MODE = "TexGen mode";
        public const string IDS_TEXTURE_BINDING = "Texture binding";
        public const string IDS_TEXTURE_DIMENSION_KEYWORD = "Texture dimension keyword";
        public const string IDS_TRUE_LITERAL = "true/on";
        public const string IDS_VALUE = "value";
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