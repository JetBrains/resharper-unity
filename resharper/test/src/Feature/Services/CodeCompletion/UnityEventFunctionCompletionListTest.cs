using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.CodeCompletion
{
    // TODO: This doesn't test automatic completion
    // The AutomaticCodeCompletionTestBase class is not in the SDK
    [TestUnity]
    public class UnityEventFunctionCompletionListTest : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"codeCompletion\List";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        [Test] public void MonoBehaviour01() { DoNamedTest(); }
        [Test] public void MonoBehaviour02() { DoNamedTest(); }
        [Test] public void MonoBehaviour03() { DoNamedTest(); }
        [Test] public void MonoBehaviour04() { DoNamedTest(); }
        [Test] public void MonoBehaviour05() { DoNamedTest(); }
        [Test] public void MonoBehaviour06() { DoNamedTest(); }
        [Test] public void MonoBehaviour07() { DoNamedTest(); }
        [Test] public void MonoBehaviour08() { DoNamedTest(); }
        [Test] public void NoCompletionInsideAttributeSectionList() { DoNamedTest(); }
        [Test] public void UnityEditor01() { DoNamedTest(); }
    }
}