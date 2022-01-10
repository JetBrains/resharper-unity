using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.UsageChecking
{
    [TestUnity]
    public class UsageInspectionsSuppressorTestDisabledYaml : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\UsageChecking";

        protected override bool DisableYamlParsing() => true;

        [Test]
        public void PotentialEventHandlerMethodsYamlDisabled()
        {
            DoNamedTest();
        }
    }
}