using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class UseExplicitTypeInsteadOfStringQuickFixAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UseExplicitTypeInsteadOfString\Availability";

        [Test] public void GetComponentAvailableTest() { DoNamedTest(); }
        [Test] public void GetComponentBuiltInComponentTest() { DoNamedTest(); }
        [Test] public void GetComponentUnavailableDueToBadInheritanceTest() { DoNamedTest(); }
        [Test] public void GetComponentUnavailableDueToBadSyntaxTest() { DoNamedTest(); }
        [Test] public void GetComponentUnavailableTest() { DoNamedTest(); }
        [Test] public void ScriptableObjectAvailableTest() { DoNamedTest(); }
    }
    
    [TestUnity]
    public class UseExplicitTypeInsteadOfStringQuickFixTest : QuickFixTestBase<UseExplicitTypeInsteadOfStringQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\UseExplicitTypeInsteadOfString";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void AddComponentOnObjectTransformationTest() { DoNamedTest(); }
        [Test] public void GetComponentBuiltInTransform() { DoNamedTest(); } 
        [Test] public void GetComponentInScriptTransformationTest() { DoNamedTest(); }
        [Test] public void GetComponentTransformationTest() { DoNamedTest(); }
        [Test] public void ScriptableObjectTest() { DoNamedTest(); }
        [Test] public void TransformWithOptionsTest01() { DoNamedTest(); }
        [Test] public void TransformWithOptionsTest02() { DoNamedTest(); }
        [Test] public void TransformWithOptionsTest03() { DoNamedTest(); }
        [Test] public void TransformWithOptionsTest04() { DoNamedTest(); }
        
    }
}