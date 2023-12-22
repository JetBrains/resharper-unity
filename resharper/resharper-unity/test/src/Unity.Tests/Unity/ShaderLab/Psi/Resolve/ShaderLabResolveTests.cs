using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Psi.Resolve
{
    [RequireHlslSupport, TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabResolveTests : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Psi\Resolve";
        protected override bool AcceptReference(IReference reference) => true;

        [Test] public void TestVariableReference01() { DoNamedTest2(); }
        
        [Test] public void TestShaderReference() => DoTestSolution("TestShaderReference01.shader", "TestShaderReference01.01.shader");
        [Test] public void TestTexturePassReference() => DoTestSolution("TestTexturePassReference01.shader", "TestTexturePassReference01.01.shader");
        [Test] public void TestShaderReferenceInCSharp() => DoTestSolution("TestShaderReferenceInCSharp01.cs", "TestShaderReference01.01.shader");
    }
}
