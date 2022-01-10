using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.GenerateMemberBody;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantInitializeOnLoadAttributeQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantInitializeOnLoadAttribute\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }

        // Test06 so we can share files between availability and action tests
        [Test] public void Test06() { DoNamedTest(); }
    }

    [TestUnity]
    public class RedundantInitializeOnLoadAttributeQuickFixRemoveTests : CSharpQuickFixTestBase<RemoveRedundantAttributeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantInitializeOnLoadAttribute";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test06() { DoNamedTest(); }
    }

    [TestUnity]
    [TestSetting(typeof(GenerateMemberBodySettings), nameof(GenerateMemberBodySettings.MethodImplementationKind), MethodImplementationKind.ThrowNotImplemented)]
    public class RedundantInitializeOnLoadAttributeQuickFixCreateTests : CSharpQuickFixTestBase<CreateFromUsageFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantInitializeOnLoadAttribute";

        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }

        [Test, TestSetting(typeof(GenerateMemberBodySettings), nameof(GenerateMemberBodySettings.MethodImplementationKind), MethodImplementationKind.ReturnDefaultValue)]
        public void Test05() { DoNamedTest(); }
    }
}