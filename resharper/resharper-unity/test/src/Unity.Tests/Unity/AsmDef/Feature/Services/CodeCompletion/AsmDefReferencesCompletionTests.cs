using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Feature.Services.CodeCompletion
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencesCompletionListTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\AsmDefReferences";

        [Test] public void TestList01() { DoTestSolution( [TestName], ["Ref01_SecondProject.asmdef"]); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencesCompletionActionTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\AsmDefReferences";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        [Test] public void TestAction01() { DoTestSolution([TestName], ["Ref01_SecondProject.asmdef"]); }
    }
}
