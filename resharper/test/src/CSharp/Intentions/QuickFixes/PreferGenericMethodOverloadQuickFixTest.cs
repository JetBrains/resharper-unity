using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class PreferGenericMethodOverloadQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferGenericMethodOverload\Availability";

        [Test] public void GetComponentAvailableTest() { DoNamedTest(); }
        [Test] public void GetComponentBuiltInComponentTest() { DoNamedTest(); }
        [Test] public void GetComponentUnavailableDueToBadSyntaxTest() { DoNamedTest(); }
        [Test] public void GetComponentUnavailableTest() { DoNamedTest(); }
        [Test] public void ScriptableObjectAvailableTest() { DoNamedTest(); }
        [Test] public void GetComponentUnavailableDueToGenericClass() { DoNamedTest(); }
        [Test] public void GetComponentWithNamespaceUnavailableTest() { DoNamedTest(); }
        [Test] public void GetComponentWithPreprocessorDirectives() { DoNamedTest(); }
    }

    [TestUnity]
    public class PreferGenericMethodOverloadQuickFixTest : QuickFixTestBase<PreferGenericMethodOverloadQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\PreferGenericMethodOverload";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void AddComponentOnObjectTransformationTest() { DoNamedTest(); }
        [Test] public void GetComponentBuiltInTransform() { DoNamedTest(); } 
        [Test] public void GetComponentInScriptTransformationTest() { DoNamedTest(); }
        [Test] public void GetComponentTransformationTest() { DoNamedTest(); }
        [Test] public void ScriptableObjectTest() { DoNamedTest(); }
        [Test] public void GetComponentWithNamespaceTest01() { DoNamedTest(); }
        [Test] public void GetComponentWithNamespaceTest02() { DoNamedTest(); }
    }
}