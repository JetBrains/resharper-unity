using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityObjectDoubleQuestionAssignmentWarningTests : CSharpHighlightingTestBase<UnityObjectDoubleQuestionAssignmentWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestUnityObjectDoubleQuestionAssignmentWarning() { DoNamedTest2(); }
    }
}
