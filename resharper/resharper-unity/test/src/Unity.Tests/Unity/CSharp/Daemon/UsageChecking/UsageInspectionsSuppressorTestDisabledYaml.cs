using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.UsageChecking
{
    [TestUnity]
    public class UsageInspectionsSuppressorTestDisabledYaml : UsageCheckBaseTest
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\UsageChecking";

        protected override bool DisableYamlParsing() => true;

        [Test]
        [UnityPluginBackendChecklist("C# code analysis", "Implicit usages", "Suppressed inspections with YAML parsing off")]
        public void PotentialEventHandlerMethodsYamlDisabled()
        {
            DoNamedTest();
        }
    }
}