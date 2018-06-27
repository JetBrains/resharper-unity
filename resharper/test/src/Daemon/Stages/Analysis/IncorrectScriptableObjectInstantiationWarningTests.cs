using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class IncorrectScriptableObjectInstantiationWarningTests : CSharpHighlightingTestBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis";

        [Test] public void TestInstantiateScriptableObjectWarning() { DoNamedTest2(); }
    }
}