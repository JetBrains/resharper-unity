using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.CodeCompletion
{
    [TestUnity(UnityVersion.Unity2022_3)]
    public class UnityUIElementsCompletionTest : CodeCompletionTestBase
    {
        private LookupListSorting mySorting = LookupListSorting.ByRelevance;

        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"UnityUIElementsCompletionTest";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => mySorting;

        protected override string SolutionFileName => SolutionItemsBasePath.Combine("Solutions/UIElementsDemo/UIElementsDemo.sln").FullPath;

        private VirtualFileSystemPath[] myFiles =>
            VirtualTestDataPath.Combine("Solutions/UIElementsDemo/")
                .GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories).ToArray();

        [Test] public void UIController01() { DoNamedTest(myFiles.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController02() { DoNamedTest(myFiles.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController03() { DoNamedTest(myFiles.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController04() { DoNamedTest(myFiles.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController05() { DoNamedTest(myFiles.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController06() { DoNamedTest(myFiles.Select(a=>a.FullPath).ToArray()); }
    }
}