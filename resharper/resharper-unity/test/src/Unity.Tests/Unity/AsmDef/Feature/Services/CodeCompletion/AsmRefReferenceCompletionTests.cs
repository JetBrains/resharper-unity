using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Feature.Services.CodeCompletion
{
    [TestUnity]
    [TestFileExtension(".asmref")]
    public class AsmRefReferenceCompletionListTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\AsmRefReference";

        [Test] public void TestList01() { DoNamedTest("Ref01_SecondProject.asmdef"); }
    }

    [TestUnity]
    [TestFileExtension(".asmref")]
    public class AsmRefReferenceCompletionActionTests : CodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"AsmDef\CodeCompletion\AsmRefReference";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        [Test] public void TestAction01() { DoNamedTest("Ref01_SecondProject.asmdef"); }
    }
}
