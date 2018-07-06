using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class InefficientCameraMainUsageWarningTests : CSharpHighlightingTestBase<InefficientCameraMainUsageWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";
        
        [Test] public void TestInefficientCameraMainUsageWarning() { DoNamedTest2(); }
    }
}