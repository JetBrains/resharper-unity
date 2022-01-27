using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Intentions.QuickFixes
{
    [RequireHlslSupport]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabRedundantPreprocessorCharQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"ShaderLab\Intentions\QuickFixes\ShaderLabRedundantPreprocessorChar\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [RequireHlslSupport]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabRedundantPreprocessorCharQuickFixTests : CSharpQuickFixTestBase<ShaderLabRedundantPreprocessorCharQuickFix>
    {
        protected override string RelativeTestDataPath=> @"ShaderLab\Intentions\QuickFixes\ShaderLabRedundantPreprocessorChar";

        [Test] public void Test01() { DoNamedTest(); }
    }
}