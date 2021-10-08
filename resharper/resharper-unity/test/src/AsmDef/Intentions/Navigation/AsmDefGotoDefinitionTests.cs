using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.IntentionsTests.Navigation;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Intentions.Navigation
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefGotoDeclarationTests : AllNavigationProvidersTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "Navigation";

        [Test] public void Test01() { DoNamedTest("Ref1.asmdef"); }
        [Test] public void Test02() { DoNamedTest("Ref1.asmdef"); }
        [Test] public void Test03() { DoNamedTest("Ref1.asmdef", "Ref2.asmdef"); }

        [Test]
        public void TestGuidReference01()
        {
            DoNamedTest2("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta");
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