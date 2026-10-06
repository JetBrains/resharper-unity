using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.CodeCompletion
{
    // TODO: This doesn't test automatic completion
    // The AutomaticCodeCompletionTestBase class is not in the SDK
    [TestUnity]
    public class UnityEventFunctionCompletionListTest : CodeCompletionTestBase
    {
        private LookupListSorting mySorting = LookupListSorting.ByRelevance;

        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
        protected override string RelativeTestDataPath => @"CSharp\CodeCompletion\List";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => mySorting;

        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour06() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour07() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void MonoBehaviour08() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void NoCompletionInsideStruct() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void NoCompletionInsideInterface() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void NoCompletionInsideAttributeSectionList() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void NoCompletionFollowingSerializeFieldAttribute01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void NoCompletionFollowingSerializeFieldAttribute02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void NoCompletionFollowingSerializeFieldAttribute03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void NoCompletionFollowingSerializeFieldAttribute04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void DoNotMatchParameterTypes() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void DoNotListVirtualFunctions() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void DoNotListFunctionsImplementedInBase01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list in unsupported contexts")] public void DoNotListFunctionsImplementedInBase02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void UnityEditor01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void EditorWindow01() { DoNamedTest(); }

        [Test]
        [UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")]
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

        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void RetypeNameOnExistingMethodWithDifferentSignature() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Code completion", "Event functions", "Completion list")] public void RetypeNameOnExistingMethodWithDifferentSignature2() { DoNamedTest(); }

        // Really useful for debugging ordering!
//        protected override void PresentLookupItem(TextWriter writer, ILookupItem lookupItem, bool showTypes)
//        {
//            base.PresentLookupItem(writer, lookupItem, showTypes);
//            writer.Write(" {0} {1} {2} {3} <{4}>", lookupItem.Placement.Relevance, lookupItem.Placement.Location,
//                lookupItem.Placement.Rank, lookupItem.Placement.SelectionPriority, lookupItem.Placement.RelevanceBits);
//        }
    }
}