using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class ConvertToGameObjectAddComponentQuickFixAvailabilityTests
        : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToGameObjectAddComponent\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Object creation", "Convert to GameObject.AddComponent")] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    public class ConvertToGameObjectAddComponentQuickFixTests
        : QuickFixTestBase<ConvertToGameObjectAddComponentQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToGameObjectAddComponent";
        protected override bool AllowHighlightingOverlap => true;

        [Test, UnityPluginBackendChecklist("Quick fixes", "Object creation", "Convert to GameObject.AddComponent")] public void Test01() { DoNamedTest(); }
    }
}