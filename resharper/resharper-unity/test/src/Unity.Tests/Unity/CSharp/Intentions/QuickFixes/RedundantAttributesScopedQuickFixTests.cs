using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantAttributesScopedQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributesScopedQuickFix\Availability";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is IUnityAnalyzerHighlighting
                   && base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore);
        }

        [Test] public void TestAllAttributes() { DoNamedTest2(); }
    }

    [TestUnity]
    public class RedundantAttributesScopedQuickFixAvailabilityGlobalStageTests : QuickFixAfterSwaAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributesScopedQuickFix\Availability";

        protected override string GetGoldTestDataPath(string fileName)
        {
            return base.GetGoldTestDataPath(fileName + ".global");
        }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is IUnityAnalyzerHighlighting
                   && base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore);
        }

        [Test] public void TestAllAttributes() { DoNamedTest2(); }
    }

    [TestUnity]
    public class RedundantAttributesScopedQuickFixTests :  CSharpQuickFixTestBase<RemoveRedundantAttributeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributesScopedQuickFix";

        [Test] public void TestAllAttributes() { DoNamedTest2(); }
    }

    // [TestUnity]
    // public class RedundantAttributesScopedQuickFixGlobalStageTests : CSharpQuickFixAfterSwaTestBase<RemoveRedundantAttributeQuickFix>
    // {
    //     protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributesScopedQuickFix";
    //
    //     protected override string GetGoldTestDataPath(string fileName)
    //     {
    //         return base.GetGoldTestDataPath(fileName + ".global");
    //     }
    //
    //     [Test] public void TestAllAttributes() { DoNamedTest2(); }
    // }
}