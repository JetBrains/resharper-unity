using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class InefficientMultidimensionalArrayUsageQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InefficientMultidimensionalArrayUsage\Availability";

        [Test] public void ErrorElement() { DoNamedTest(); }
        [Test] public void FieldWithoutInitializer() { DoNamedTest(); }
        [Test] public void PublicFieldWithoutInitializer() { DoNamedTest(); }
        [Test] public void AdditionalUsages() { DoNamedTest(); }
        [Test] public void PrivateFieldWithUsage() { DoNamedTest(); }

        [Test] public void MultipleDeclarators() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return base.HighlightingPredicate(highlighting, psiSourceFile, boundSettingsStore) &&
                   highlighting is InefficientMultidimensionalArrayUsageWarning;
        }
    }

    [TestUnity]
    public class InefficientMultidimensionalArrayUsageQuickFixTests : CSharpQuickFixTestBase<InefficientMultidimensionalArrayUsageQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InefficientMultidimensionalArrayUsage";

        [Test] public void LocalDeclarationVar() { DoNamedTest(); }
        [Test] public void LocalDeclarationType() { DoNamedTest(); }
        [Test] public void LocalDeclarationType2() { DoNamedTest(); }
        [Test] public void LocalDeclarationWithInitializer() { DoNamedTest(); }
    }
}