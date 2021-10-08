using System.Collections.Generic;
using System.Linq;
using JetBrains.DocumentManagers;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.Framework;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.Refactorings
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefRenameTests : RenameTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Refactorings\Rename";

        [Test] public void TestSingleFile() { DoNamedTest2(); }
        [Test] public void TestCrossFileRename() { DoNamedTest2("CrossFileRename_SecondProject.asmdef"); }
        [Test] public void TestRenameFile() { DoNamedTest2(); }
        [Test] public void TestGuidReference() { DoNamedTest2("GuidReference.asmdef.meta", "GuidReference_SecondProject.asmdef"); }

        protected override void AdditionalTestChecks(ITextControl textControl, IProject project)
        {
            var solution = project.GetSolution();
            foreach (var topLevelProject in solution.GetTopLevelProjects())
            {
                if (topLevelProject.IsProjectFromUserView() && !Equals(topLevelProject, project))
                {
                    foreach (var projectFile in topLevelProject.GetSubItems().OfType<IProjectFile>())
                    {
                        ExecuteWithGold(projectFile, writer =>
                        {
                            var document = projectFile.GetDocument();
                            writer.Write(document.GetText());
                        });
                    }

                    // TODO: Should really recurse into child folders, but not used by these tests
                }
            }
        }

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