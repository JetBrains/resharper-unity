using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public class ShaderLabRedundantPreprocessorCharQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\ShaderLabRedundantPreprocessorChar\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public class ShaderLabRedundantPreprocessorCharQuickFixQuickFixTests : CSharpQuickFixTestBase<ShaderLabRedundantPreprocessorCharQuickFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\ShaderLabRedundantPreprocessorChar";

        [Test] public void Test01() { DoNamedTest(); }
    }
}