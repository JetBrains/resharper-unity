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

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void ErrorElement() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void FieldWithoutInitializer() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void PublicFieldWithoutInitializer() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void AdditionalUsages() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void PrivateFieldWithUsage() { DoNamedTest(); }

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void MultipleDeclarators() { DoNamedTest(); }

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

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void LocalDeclarationVar() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void LocalDeclarationType() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void LocalDeclarationType2() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Inefficient multidimensional array usage")] public void LocalDeclarationWithInitializer() { DoNamedTest(); }
    }
}