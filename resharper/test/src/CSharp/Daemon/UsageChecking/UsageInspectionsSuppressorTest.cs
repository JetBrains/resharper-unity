using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.UsageChecking
{
    [TestUnity]
    public class UsageInspectionsSuppressorTest : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\UsageChecking";

        [Test] public void MonoBehaviourMethods01() { DoNamedTest(); }
        [Test] public void MonoBehaviourFields01() { DoNamedTest(); }
    }
}