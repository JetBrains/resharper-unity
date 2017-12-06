using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    [CSharpLanguageLevel(CSharpLanguageLevel.CSharp40)]
    public class UseSerializedBackingFieldFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\UseSerializedBackingField\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test, CSharpLanguageLevel(CSharpLanguageLevel.CSharp60)] public void Test02() { DoNamedTest(); }
    }

    [TestUnity]
    [CSharpLanguageLevel(CSharpLanguageLevel.CSharp40)]
    public class UseSerializedBackingFieldFixRemoveTests : CSharpQuickFixTestBase<UseSerializedBackingFieldFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\UseSerializedBackingField";

        [Test] public void Test01() { DoNamedTest(); }
        [Test, CSharpLanguageLevel(CSharpLanguageLevel.CSharp60)] public void Test02() { DoNamedTest(); }
    }
}