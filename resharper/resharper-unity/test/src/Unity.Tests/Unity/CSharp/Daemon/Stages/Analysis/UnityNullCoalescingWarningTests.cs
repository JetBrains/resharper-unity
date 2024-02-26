using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    [TestSetting(typeof(UnitySettings), nameof(UnitySettings.ForceLifetimeChecks), true)]
    public class UnityNullCoalescingWarningTests : CSharpHighlightingTestBase<UnityObjectNullCoalescingWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestUnityNullCoalescingWarning() { DoNamedTest2(); }
    }
}
