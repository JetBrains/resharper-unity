package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.ide.highlighter.custom.SyntaxTable
import com.intellij.openapi.editor.DefaultLanguageHighlighterColors
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.psi.CustomHighlighterTokenType
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.colors.RiderLanguageTextAttributeKeys

class CgKeywords(isMultilineCommentEnabled: Boolean) {
    val appdata = listOf("appdata_base", "appdata_full", "appdata_img", "appdata_tan")
    val types = listOf("bool", "fixed", "fixed1x1", "fixed1x2", "fixed1x3", "fixed1x4", "fixed2", "fixed2x1", "fixed2x2", "fixed2x3", "fixed2x4", "fixed3", "fixed3x1", "fixed3x2", "fixed3x3", "fixed3x4", "fixed4", "fixed4x1", "fixed4x2", "fixed4x3", "fixed4x4", "float", "float1x1", "float1x2", "float1x3", "float1x4", "float2", "float2x1", "float2x2", "float2x3", "float2x4", "float3", "float3x1", "float3x2", "float3x3", "float3x4", "float4", "float4x1", "float4x2", "float4x3", "float4x4", "half", "half1x1", "half1x2", "half1x3", "half1x4", "half2", "half2x1", "half2x2", "half2x3", "half2x4", "half3", "half3x1", "half3x2", "half3x3", "half3x4", "half4", "half4x1", "half4x2", "half4x3", "half4x4", "int", "int1x1", "int1x2", "int1x3", "int1x4", "int2", "int2x1", "int2x2", "int2x3", "int2x4", "int3", "int3x1", "int3x2", "int3x3", "int3x4", "int4", "int4x1", "int4x2", "int4x3", "int4x4")
    val sampler = listOf("sampler1D", "sampler2D", "sampler3D", "samplerCUBE", "samplerRECT")
    val color = listOf("COLOR", "COLOR0", "COLOR1", "COLOR2", "COLOR3", "COLOR4", "COLOR5", "COLOR6", "COLOR7")
    val sv = listOf("SV_Depth", "SV_POSITION", "SV_Target", "SV_Target1", "SV_Target2", "SV_Target3", "SV_Target4", "SV_Target5", "SV_Target6", "SV_Target7", "SV_VertexID")
    val texcoord = listOf("TEXCOORD0", "TEXCOORD1", "TEXCOORD2", "TEXCOORD3", "TEXCOORD4", "TEXCOORD5", "TEXCOORD6", "TEXCOORD7")
    val unityUpperCaseTypes = listOf("UNITY_LIGHTMODEL_AMBIENT", "UNITY_MATRIX_IT_MV", "UNITY_MATRIX_MV", "UNITY_MATRIX_MVP", "UNITY_MATRIX_P", "UNITY_MATRIX_T_MV", "UNITY_MATRIX_V", "UNITY_MATRIX_VP", "UNITY_PI")
    val unityLowerCaseTypes = listOf("unity_4LightAtten0", "unity_4LightPosX0", "unity_4LightPosY0", "unity_4LightPosZ0", "unity_AmbientEquator", "unity_AmbientGround", "unity_AmbientSky", "unity_CameraInvProjection", "unity_CameraProjection", "unity_CameraWorldClipPlanes", "unity_DeltaTime", "unity_FogColor", "unity_FogParams", "unity_LODFade", "unity_LightAtten", "unity_LightColor", "unity_LightPosition", "unity_OrthoParams", "unity_SpotDirection")
    val miscTypes = listOf("POSITION", "TANGENT")

    val cgKeywords = listOf("break", "case", "catch", "const", "continue", "default", "do", "else", "extern", "for", "goto", "if", "in", "inline", "inout", "out", "return", "static", "struct", "switch", "throw", "try", "uniform", "varying", "while")
    val directives = listOf("#include", "#pragma", "#define", "#elif", "#else", "#elseif", "#endif", "#if", "#ifdef", "#ifndef")

    val semanticInputVariables = listOf("VFACE", "VPOS")

    val miscMacros = listOf("CBUFFER_END", "CBUFFER_START")

    val shaderApi = listOf("SHADER_API_D3D11", "SHADER_API_D3D11_9X", "SHADER_API_D3D9", "SHADER_API_GLCORE", "SHADER_API_GLES", "SHADER_API_GLES3", "SHADER_API_METAL", "SHADER_API_MOBILE", "SHADER_API_OPENGL", "SHADER_API_PSP2", "SHADER_API_PSSL", "SHADER_API_WIIU", "SHADER_API_XBOX360", "SHADER_API_XBOXONE")
    val shaderTarget = listOf("SHADER_TARGET", "SHADER_TARGET_GLSL")
    val unityConst = listOf("UNITY_ATTEN_CHANNEL", "UNITY_BRANCH", "UNITY_CAN_COMPILE_TESSELLATION", "UNITY_COMPILER_CG", "UNITY_COMPILER_HLSL", "UNITY_COMPILER_HLSL2GLSL", "UNITY_DECLARE_SHADOWMAP", "UNITY_DECLARE_TEX2D", "UNITY_DECLARE_TEX2DARRAY", "UNITY_DECLARE_TEX2D_NOSAMPLER", "UNITY_FLATTEN", "UNITY_FRAMEBUFFER_FETCH_AVAILABLE", "UNITY_HALF_TEXEL_OFFSET", "UNITY_INITIALIZE_OUTPUT", "UNITY_MIGHT_NOT_HAVE_DEPTH_Texture", "UNITY_NEAR_CLIP_VALUE", "UNITY_NO_DXT5nm", "UNITY_NO_LINEAR_COLORSPACE", "UNITY_NO_RGBM", "UNITY_NO_SCREENSPACE_SHADOWS", "UNITY_PASS_DEFERRED", "UNITY_PASS_FORWARDADD", "UNITY_PASS_FORWARDBASE", "UNITY_PASS_PREPASSBASE", "UNITY_PASS_PREPASSFINAL", "UNITY_PASS_SHADOWCASTER", "UNITY_PROJ_COORD", "UNITY_SAMPLE_SHADOW", "UNITY_SAMPLE_SHADOW_PROJ", "UNITY_SAMPLE_TEX2D", "UNITY_SAMPLE_TEX2DARRAY", "UNITY_SAMPLE_TEX2DARRAY_LOD", "UNITY_SAMPLE_TEX2D_SAMPLER", "UNITY_USE_RGBA_FOR_POINT_SHADOWS", "UNITY_UV_STARTS_AT_TOP", "UNITY_VERSION", "UNITY_VPOS_TYPE")

    val table = SyntaxTable()

    init {
        table.isIgnoreCase = false
        table.keywords1.addAll(appdata)
        table.keywords1.addAll(types)
        table.keywords1.addAll(sampler)
        table.keywords1.addAll(color)
        table.keywords1.addAll(sv)
        table.keywords1.addAll(texcoord)
        table.keywords1.addAll(unityUpperCaseTypes)
        table.keywords1.addAll(unityLowerCaseTypes)
        table.keywords1.addAll(miscTypes)

        table.keywords2.addAll(cgKeywords)
        table.keywords2.addAll(directives)

        table.keywords3.addAll(semanticInputVariables)

        table.keywords4.addAll(miscMacros)
        table.keywords4.addAll(shaderApi)
        table.keywords4.addAll(shaderTarget)
        table.keywords4.addAll(unityConst)

        table.isHasBrackets = true
        table.isHasParens = true
        table.isHasBraces = true
        table.lineComment = "//"

        if (isMultilineCommentEnabled){
            table.startComment = "/*"
            table.endComment = "*/"
        }
    }

    val tokenToHighlightMap: Map<IElementType, TextAttributesKey> = mapOf(
        CustomHighlighterTokenType.KEYWORD_1 to RiderLanguageTextAttributeKeys.CLASS,
        CustomHighlighterTokenType.KEYWORD_2 to RiderLanguageTextAttributeKeys.KEYWORD,
        CustomHighlighterTokenType.KEYWORD_3 to RiderLanguageTextAttributeKeys.KEYWORD,
        CustomHighlighterTokenType.KEYWORD_4 to DefaultLanguageHighlighterColors.CONSTANT
    )
}