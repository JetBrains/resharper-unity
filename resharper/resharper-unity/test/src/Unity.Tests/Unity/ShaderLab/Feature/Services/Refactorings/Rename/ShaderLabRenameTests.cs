using JetBrains.ReSharper.FeaturesTestFramework.Refactorings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.Refactorings.Rename
{
    [TestUnity, TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabRenameTests : RenameTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Refactorings\Rename";
        
        [Test] public void TestRenameShaderWithReferenceFromReference() => DoTestSolution("TestShader01.shader", "TestShader01.01.shader");
        [Test] public void TestRenameShaderWithReferenceFromDeclaration() => DoTestSolution("TestShader02.01.shader", "TestShader02.shader");
        [Test] public void TestRenameShaderWithReferenceFromPassReference() => DoTestSolution("TestShader03.01.shader", "TestShader03.shader");
        [Test] public void TestRenameTexturePassFromDeclaration() => DoTestSolution("TestTexturePassRenameFromDeclaration.01.shader", "TestTexturePassRenameFromDeclaration.shader");
        [Test] public void TestRenameTexturePassFromUsage() => DoTestSolution("TestTexturePassRenameFromUsage.01.shader", "TestTexturePassRenameFromUsage.shader");
        [Test] public void TestRenameShaderWithReferenceInCSharp() => DoTestSolution("TestShaderReferenceRenameInCSharp01.cs", "TestShaderReferenceRenameInCSharp01.01.shader");
    }
}