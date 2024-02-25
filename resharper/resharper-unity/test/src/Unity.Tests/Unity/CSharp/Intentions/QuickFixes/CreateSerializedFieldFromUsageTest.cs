using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class CreateSerializedFieldFromUsageTest
        : CSharpQuickFixTestBase<CreateFromUsageFix, CreateSerializedFieldFromUsageAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\CreateFromUsage";

        [Test] public void TestSerializedField01() { DoNamedTest2(); }
        [Test] public void TestSerializedField02() { DoNamedTest2(); }
        [Test] public void TestSerializedField03() { DoNamedTest2(); }
        [Test] public void TestSerializedField04() { DoNamedTest2(); }
    }
}