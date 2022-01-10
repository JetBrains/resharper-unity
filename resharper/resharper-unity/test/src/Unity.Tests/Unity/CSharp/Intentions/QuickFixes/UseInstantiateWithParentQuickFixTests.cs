using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class UseInstantiateWithParentQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UseInstantiateWithParent\Availability";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is IUnityHighlighting
                   && base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore);
        }

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void SetParentWithFalseTest() { DoNamedTest(); }
        [Test] public void SetParentWithTrueTest() { DoNamedTest(); }
        [Test] public void SetParentByPropertyTest() { DoNamedTest(); }
        [Test] public void SomethingBetweenTest() { DoNamedTest(); }
        [Test] public void ScopedTest() { DoNamedTest(); }
    }

    [TestUnity]
    public class UseInstantiateWithParentQuickFixTests : QuickFixTestBase<UseInstantiateWithParentQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UseInstantiateWithParent";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void SimpleTest() { DoNamedTest(); }
        [Test] public void SetParentWithFalseTest() { DoNamedTest(); }
        [Test] public void SetParentWithTrueTest() { DoNamedTest(); }
        [Test] public void SetParentByPropertyTest() { DoNamedTest(); }
        [Test] public void SomethingBetweenTest() { DoNamedTest(); }
        [Test, ExecuteScopedQuickFixInFile] public void ScopedTest() { DoNamedTest(); }
    }
}