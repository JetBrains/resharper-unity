using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.TestFramework.Projects;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.CodeCompletion
{
    [TestUnity(UnityVersion.Unity2022_3), ReuseSolutionScope("UnityUIElementsCompletionTest")]
    public class UnityUIElementsCompletionTest : CodeCompletionTestBase
    {
        // test solution for manual testing 
        // https://jetbrains.team/p/dotnettestprojects/repositories/UIElementsDemo/

        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"UnityUIElementsCompletionTest";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        protected override string SolutionFileName => SolutionItemsBasePath.Combine("Solutions/UIElementsDemo/UIElementsDemo.sln").FullPath;

        private VirtualFileSystemPath[] Files =>
            VirtualTestDataPath.Combine("Solutions/UIElementsDemo/")
                .GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories).ToArray();
        
        [Test] public void UIController01() { DoNamedTest(Files.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController02() { DoNamedTest(Files.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController03() { DoNamedTest(Files.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController04() { DoNamedTest(Files.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController05() { DoNamedTest(Files.Select(a=>a.FullPath).ToArray()); }
        [Test] public void UIController06() { DoNamedTest(Files.Select(a=>a.FullPath).ToArray()); }
    }
}