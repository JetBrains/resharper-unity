using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabRedundantPreprocessorCharQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\ShaderLabRedundantPreprocessorChar\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabRedundantPreprocessorCharQuickFixQuickFixTests : CSharpQuickFixTestBase<ShaderLabRedundantPreprocessorCharQuickFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\ShaderLabRedundantPreprocessorChar";

        [Test] public void Test01() { DoNamedTest(); }
    }
}