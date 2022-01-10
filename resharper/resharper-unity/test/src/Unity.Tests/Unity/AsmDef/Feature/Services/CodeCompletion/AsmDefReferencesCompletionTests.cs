using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.CodeCompletion
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencesCompletionListTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\AsmDefReferences";

        [Test] public void TestList01() { DoNamedTest("Ref01_SecondProject.asmdef"); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencesCompletionActionTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\AsmDefReferences";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        [Test] public void TestAction01() { DoNamedTest("Ref01_SecondProject.asmdef"); }
    }

    public abstract class TwoProjectCodeCompletionTestBase : CodeCompletionTestBase
    {
        // Sadly, we can't just use DoTestSolution(fileSet, fileSet) here, CodeCompletionTestBase.DoTestSolution(files)
        // sets up a CaretPositionsProcessor and processes files. Split the file sets here instead
        protected override TestSolutionConfiguration CreateSolutionConfiguration(
            ICollection<KeyValuePair<TargetFrameworkId, IEnumerable<string>>> referencedLibraries,
            IEnumerable<string> fileSet)
        {
            var files = fileSet.ToList();
            var mainFileSet = files.Where(f => !f.Contains("_SecondProject"));
            var secondaryFileSet = files.Where(f => f.Contains("_SecondProject"));
            return base.CreateSolutionConfiguration(referencedLibraries,
                CreateProjectFileSets(mainFileSet, secondaryFileSet));
        }
    }
}
