using System.IO;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services.CodeCompletion
{
    // TODO: This doesn't test automatic completion
    // The AutomaticCodeCompletionTestBase class is not in the SDK
    [TestUnity]
    public class UnityEventFunctionCompletionListTest : CodeCompletionTestBase
    {
        private LookupListSorting mySorting = LookupListSorting.ByRelevance;

        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"CSharp\CodeCompletion\List";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => mySorting;

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
        [Test] public void EditorWindow01() { DoNamedTest(); }

        [Test]
        public void AlphabeticalMonoBehaviour01()
        {
            mySorting = LookupListSorting.Alphabetically;
            try
            {
                DoNamedTest();
            }
            finally
            {
                mySorting = LookupListSorting.ByRelevance;
            }
        }

        // Really useful for debugging ordering!
//        protected override void PresentLookupItem(TextWriter writer, ILookupItem lookupItem, bool showTypes)
//        {
//            base.PresentLookupItem(writer, lookupItem, showTypes);
//            writer.Write(" {0} {1} {2} {3} <{4}>", lookupItem.Placement.Relevance, lookupItem.Placement.Location,
//                lookupItem.Placement.Rank, lookupItem.Placement.SelectionPriority, lookupItem.Placement.RelevanceBits);
//        }
    }
}