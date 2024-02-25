using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class CreateMethodFromUnityStringLiteralUsageTest
        : CSharpQuickFixTestBase<CreateFromUsageFix, UnityCreateMethodFromStringLiteralUsageAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\CreateFromUsage";

        [Test] public void TestCreateMethodForCoroutine() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine01() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine02() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine03() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine04() { DoNamedTest2(); }
        [Test] public void TestCreateMethodForCoroutine05() { DoNamedTest2(); }
    }
}