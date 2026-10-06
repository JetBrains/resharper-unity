using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class FormerlySerializedAsSplitDeclarationsFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\FormerlySerializedAsSplitDeclarations\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Split declarations for FormerlySerializedAs")] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    public class FormerlySerializedAsSplitDeclarationsFixRemoveTests : CSharpQuickFixTestBase<FormerlySerializedAsSplitDeclarationsFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\FormerlySerializedAsSplitDeclarations";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Split declarations for FormerlySerializedAs")] public void Test01() { DoNamedTest(); }
    }
}