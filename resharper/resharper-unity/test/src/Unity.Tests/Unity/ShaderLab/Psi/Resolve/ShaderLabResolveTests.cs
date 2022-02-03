using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Psi.Resolve
{
    [RequireHlslSupport]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabResolveTests : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Psi\Resolve";
        protected override bool AcceptReference(IReference reference) => true;

        [Test] public void TestVariableReference01() { DoNamedTest2(); }
    }
}
