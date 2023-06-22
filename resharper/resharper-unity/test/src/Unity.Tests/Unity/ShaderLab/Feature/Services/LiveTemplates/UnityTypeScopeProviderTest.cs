using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.LiveTemplates
{
    [TestUnity, TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabScopeProviderTest : BaseScopeProviderTest
    {
        protected override string RelativeTestDataPath => @"ShaderLab\LiveTemplates\Scope";
        protected override IScopeProvider CreateScopeProvider() => new UnityShaderLabScopeProvider();

        [Test] public void TestInShaderLabFile01() { DoNamedTest2(); }
        [Test] public void TestInShaderLabFile02() { DoNamedTest2(); }
        [Test] public void TestMustBeInShaderBlock01() { DoNamedTest2(); }
        [Test] public void TestMustBeInPropertiesBlock01() { DoNamedTest2(); }
        [Test] public void TestMustBeInTexturePassBlock01() { DoNamedTest2(); }
    }
}