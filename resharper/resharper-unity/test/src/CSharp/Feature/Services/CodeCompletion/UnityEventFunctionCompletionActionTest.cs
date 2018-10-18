using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services.CodeCompletion
{
    [TestUnity]
    public class UnityEventFunctionCompletionActionTest : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"CSharp\CodeCompletion\Action";

        [Test] public void MonoBehaviour01() { DoNamedTest(); }
        [Test] public void MonoBehaviour02() { DoNamedTest(); }
        [Test] public void MonoBehaviour03() { DoNamedTest(); }
        [Test] public void MonoBehaviour04() { DoNamedTest(); }
        [Test] public void MonoBehaviour05() { DoNamedTest(); }
        [Test] public void MonoBehaviour06() { DoNamedTest(); }
        [Test] public void MonoBehaviour07() { DoNamedTest(); }
        [Test] public void MonoBehaviour08() { DoNamedTest(); }
        [Test] public void MonoBehaviour09() { DoNamedTest(); }
    }
}