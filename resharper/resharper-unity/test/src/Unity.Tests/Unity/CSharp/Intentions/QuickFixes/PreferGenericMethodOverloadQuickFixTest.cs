using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PreferGenericMethodOverloadQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferGenericMethodOverload\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentAvailableTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentBuiltInComponentTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentUnavailableDueToBadSyntaxTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentUnavailableTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void ScriptableObjectAvailableTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentUnavailableDueToGenericClass() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentWithNamespaceUnavailableTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentWithPreprocessorDirectives() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void AllScopedTest() { DoNamedTest(); }
    }

    [TestUnity]
    public class PreferGenericMethodOverloadQuickFixTest : QuickFixTestBase<PreferGenericMethodOverloadQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferGenericMethodOverload";
        protected override bool AllowHighlightingOverlap => true;

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void AddComponentOnObjectTransformationTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentBuiltInTransform() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentInScriptTransformationTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentTransformationTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void ScriptableObjectTest() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentWithNamespaceTest01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void GetComponentWithNamespaceTest02() { DoNamedTest(); }
        [Test, ExecuteScopedActionInFile, UnityPluginBackendChecklist("Quick fixes", "Performance", "Prefer generic method overload")] public void AllScopedTest() { DoNamedTest(); }
    }
}