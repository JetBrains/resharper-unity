using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Daemon.ContextHighlighters
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefUsageContextHighlighterTests : ContextHighlighterTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AsmDefReferences";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }

        [Test]
        public void TestGuidReference01()
        {
            // In order to test with a second project, we have to add all files to the first fileset, so we call the
            // right overload of DoTestSolution to make the test work. We split into projects in
            // CreateSolutionConfiguration. We must also use a single unnamed caret in the test (`{caret}`) or the test
            // won't know which file to open in the text control
            DoTestSolution("GuidReference01.asmdef", "GuidReference_SecondProject.asmdef",
                "GuidReference_SecondProject.asmdef.meta");
        }

        [Test]
        public void TestAsmRef()
        {
            DoTestSolution("AsmRefReference01.asmref", "AsmRefDefinition01.asmdef");
        }

        [Test]
        public void TestAsmRefGuid()
        {
            DoTestSolution("AsmRefGuidReference01.asmref", "AsmRefDefinition01.asmdef", "AsmRefDefinition01.asmdef.meta");
        }

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