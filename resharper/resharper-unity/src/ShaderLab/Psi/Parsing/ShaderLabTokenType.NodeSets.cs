using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabTokenType
    {
        public static readonly NodeTypeSet KEYWORDS;

        static ShaderLabTokenType()
        {
            KEYWORDS = new NodeTypeSet(
                SHADER_KEYWORD,
                PROPERTIES_KEYWORD,
                CATEGORY_KEYWORD,
                SUB_SHADER_KEYWORD,
                FALLBACK_KEYWORD,
                CUSTOM_EDITOR_KEYWORD,
                DEPENDENCY_KEYWORD,

                COLOR_KEYWORD,
                FLOAT_KEYWORD,
                INT_KEYWORD,
                RANGE_KEYWORD,
                VECTOR_KEYWORD,

                ANY_KEYWORD,
                CUBE_KEYWORD,
                CUBE_ARRAY_KEYWORD,
                RECT_KEYWORD,
                TEXTURE_2D_KEYWORD,
                TEXTURE_2D_ARRAY_KEYWORD,
                TEXTURE_3D_KEYWORD,

                TAGS_KEYWORD,
                PASS_KEYWORD,
                USEPASS_KEYWORD,
                GRABPASS_KEYWORD,

                CULL_KEYWORD,
                ZCLIP_KEYWORD,
                ZTEST_KEYWORD,
                ZWRITE_KEYWORD,
                OFFSET_KEYWORD,
                BLEND_KEYWORD,
                BLEND_OP_KEYWORD,
                ALPHA_TO_MASK_KEYWORD,
                COLOR_MASK_KEYWORD,
                STENCIL_KEYWORD,
                NAME_KEYWORD,
                LOD_KEYWORD,
                BIND_CHANNELS_KEYWORD,

                BACK_KEYWORD,
                FRONT_KEYWORD,

                LIGHTING_KEYWORD,
                MATERIAL_KEYWORD,
                SEPARATE_SPECULAR_KEYWORD,
                COLOR_MATERIAL_KEYWORD,
                FOG_KEYWORD,
                ALPHA_TEST_KEYWORD,
                SET_TEXTURE_KEYWORD,

                DIFFUSE_KEYWORD,
                AMBIENT_KEYWORD,
                SPECULAR_KEYWORD,
                SHININESS_KEYWORD,

                COMBINE_KEYWORD,
                CONSTANT_COLOR_KEYWORD,
                MATRIX_KEYWORD,
                LIGHTMAP_MODE_KEYWORD,
                TEX_GEN_KEYWORD,

                PREVIOUS_KEYWORD,
                PRIMARY_KEYWORD,
                TEXTURE_KEYWORD,
                CONSTANT_KEYWORD,
                DOUBLE_KEYWORD,
                QUAD_KEYWORD,
                LERP_KEYWORD,
                ONE_KEYWORD,
                ALPHA_KEYWORD,
                DOT3_KEYWORD,
                DOT3RGBA_KEYWORD,

                SPHERE_MAP_KEYWORD,
                EYE_LINEAR_KEYWORD,
                CUBE_REFLECT_KEYWORD,
                CUBE_NORMAL_KEYWORD,
                OBJECT_LINEAR_KEYWORD,

                BIND_KEYWORD,

                AMBIENT_AND_DIFFUSE_KEYWORD,
                EMISSION_KEYWORD,

                MODE_KEYWORD,
                DENSITY_KEYWORD,

                GLOBAL_KEYWORD,
                LINEAR_KEYWORD,
                EXP_KEYWORD,
                EXP2_KEYWORD,

                REF_KEYWORD,
                READ_MASK_KEYWORD,
                WRITE_MASK_KEYWORD,
                PASS_FRONT_KEYWORD,
                PASS_BACK_KEYWORD,
                COMP_KEYWORD,
                COMP_FRONT_KEYWORD,
                COMP_BACK_KEYWORD,
                FAIL_KEYWORD,
                FAIL_FRONT_KEYWORD,
                FAIL_BACK_KEYWORD,
                ZFAIL_KEYWORD,
                ZFAIL_FRONT_KEYWORD,
                ZFAIL_BACK_KEYWORD,

                GREATER_KEYWORD,
                GEQUAL_KEYWORD,
                LESS_KEYWORD,
                LEQUAL_KEYWORD,
                EQUAL_KEYWORD,
                NOT_EQUAL_KEYWORD,
                ALWAYS_KEYWORD,
                NEVER_KEYWORD,

                KEEP_KEYWORD,
                ZERO_KEYWORD,
                REPLACE_KEYWORD,
                INCR_SAT_KEYWORD,
                DECR_SAT_KEYWORD,
                INVERT_KEYWORD,
                INCR_WRAP_KEYWORD,
                DECR_WRAP_KEYWORD,

                TRUE_KEYWORD,
                FALSE_KEYWORD,
                ON_KEYWORD,
                OFF_KEYWORD
              );
        }
    }
}