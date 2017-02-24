using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.ShaderLab.Parsing
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public class ParserTests : ParserTestBase<ShaderLabLanguage>
    {
        protected override string RelativeTestDataPath => @"psi\shaderLab\parsing";

        [TestCase("First")]

        [TestCase("PropertiesEmpty")]
        [TestCase("Properties")]
        [TestCase("PropertiesWithAttributes")]
        [TestCase("PropertiesWithErrors")]

        [TestCase("FallbackNamed")]
        [TestCase("FallbackNone")]
        [TestCase("FallbackError")]

        [TestCase("CustomEditor")]
        [TestCase("CustomEditorError")]

        [TestCase("Dependency01")]
        [TestCase("Dependency02")]
        [TestCase("DependencyErrors")]

        [TestCase("SubShader01")]
        [TestCase("SubShader02")]
        [TestCase("SubShaderTags")]

        [TestCase("PassDefGrabPass")]
        [TestCase("PassDefUsePass")]

        [TestCase("CullDepth01")]
        [TestCase("PassTags")]
        [TestCase("Stencil01")]
        [TestCase("Stencil02")]
        [TestCase("Name")]
        [TestCase("LOD")]

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
        public void TestParser(string name) => DoOneTest(name);
    }
}
