using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class ConvertToScriptableObjectCreateInstanceQuickFixAvailabilityTests
        : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToScriptableObjectCreateInstance\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest("MyScriptableObject.cs"); }
    }

    [TestUnity]
    public class ConvertToScriptableObjectCreateInstanceQuickFixTests
        : QuickFixTestBase<ConvertToScriptableObjectCreateInstanceQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToScriptableObjectCreateInstance";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest("MyScriptableObject.cs"); }
    }
}