using JetBrains.ReSharper.Daemon.CSharp.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityNullImplicitCastTest : CSharpHighlightingTestBase<PossibleNullReferenceExceptionWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("C# code analysis", "Unity object null checks", "Implicit cast to bool")] public void TestUnityNullImplicitCastTest() { DoNamedTest2(); }
    }
}