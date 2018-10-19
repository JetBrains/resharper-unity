using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class InvalidParametersOnVariableReferenceQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"ShaderLab\Intentions\QuickFixes\InvalidParametersOnVariableReference\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class InvalidParametersOnVariableReferenceQuickFixTests : CSharpQuickFixTestBase<InvalidParametersOnVariableReferenceQuickFix>
    {
        protected override string RelativeTestDataPath=> @"ShaderLab\Intentions\QuickFixes\InvalidParametersOnVariableReference";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
}