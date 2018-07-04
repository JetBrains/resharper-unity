using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    // Note that these test the quick fix, NOT the context action. They apply when there is an error in a property that
    // can be fixed by rewriting it with a backing field, e.g. an auto property missing a setter
    [TestUnity]
    [CSharpLanguageLevel(CSharpLanguageLevel.CSharp40)]
    public class UseSerializedBackingFieldFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\UseSerializedBackingField\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test, CSharpLanguageLevel(CSharpLanguageLevel.CSharp60)] public void Test02() { DoNamedTest(); }
    }

    [TestUnity]
    [CSharpLanguageLevel(CSharpLanguageLevel.CSharp40)]
    public class UseSerializedBackingFieldFixRemoveTests : CSharpQuickFixTestBase<UseSerializedBackingFieldFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\UseSerializedBackingField";

        [Test] public void Test01() { DoNamedTest(); }
        [Test, CSharpLanguageLevel(CSharpLanguageLevel.CSharp60)] public void Test02() { DoNamedTest(); }
    }
}