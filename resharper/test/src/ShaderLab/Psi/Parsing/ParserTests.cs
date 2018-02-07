using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Psi.Parsing
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ParserTests : ParserTestBase<ShaderLabLanguage>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Psi\Parsing";

        [TestCase("First")]

        [TestCase("ShaderWithErrors01")]
        [TestCase("ShaderWithErrors02")]
        [TestCase("ShaderWithErrors03")]

        [TestCase("PropertiesEmpty")]
        [TestCase("Properties")]
        [TestCase("PropertiesWithAttributes")]
        [TestCase("PropertiesWithErrors")]
        [TestCase("PropertiesTextureValue")]

        [TestCase("VariableReferences")]

        [TestCase("FallbackNamed")]
        [TestCase("FallbackNone")]
        [TestCase("FallbackError")]
        [TestCase("FallbackLodValueProper")]
        [TestCase("FallbackLodValueExpectedLiteral")]
        [TestCase("FallbackLodValueErrorNoLiteral")]
        [TestCase("FallbackLodValueErrorWithOff")]

        [TestCase("CustomEditor")]
        [TestCase("CustomEditorError")]

        [TestCase("Dependency01")]
        [TestCase("Dependency02")]
        [TestCase("DependencyErrors")]

        [TestCase("SubShader01")]
        [TestCase("SubShader02")]
        [TestCase("SubShader03")]
        [TestCase("SubShaderTags")]

        [TestCase("PassDefGrabPass")]
        [TestCase("PassDefUsePass")]

        [TestCase("CullDepth01")]
        [TestCase("PassTags")]
        [TestCase("Stencil01")]
        [TestCase("Stencil02")]
        [TestCase("ColorMask")]
        [TestCase("Name")]
        [TestCase("LOD")]

        [TestCase("Blending")]
        [TestCase("BlendOp")]

        [TestCase("LegacyLighting01")]
        [TestCase("LegacyLighting02")]
        [TestCase("LegacyLighting03")]
        [TestCase("LegacyLighting04")]

        [TestCase("LegacyTextureCombiner01")]
        [TestCase("LegacyTextureCombiner02")]
        [TestCase("LegacyTextureCombiner03")]
        [TestCase("LegacyTextureCombiner04")]

        [TestCase("LegacyAlphaTesting01")]
        [TestCase("LegacyAlphaTesting02")]
        [TestCase("LegacyAlphaTesting03")]
        [TestCase("LegacyAlphaTesting04")]

        [TestCase("LegacyFog")]

        [TestCase("LegacyBindChannels")]

        [TestCase("Preprocessor")]

        [TestCase("TagDeclaration")]

        [TestCase("CgInclude")]
        [TestCase("GlslInclude")]
        [TestCase("HlslInclude")]
        public void TestParser(string name) => DoOneTest(name);
    }
}
