using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.DocumentManagers;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Update;
using JetBrains.ReSharper.Plugins.Unity.Tests.Framework;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TextControl;
using JetBrains.Util;
using Microsoft.Build.Evaluation;
using NUnit.Framework;
using PlatformID = JetBrains.Application.platforms.PlatformID;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Feature.Services.Refactorings
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefRenameTests : RenameTestBase
    {
        protected override string RelativeTestDataPath => @"Json\Refactorings\Rename";

        [Test] public void TestSingleFile() { DoNamedTest2(); }
        [Test] public void TestCrossFileRename() { DoNamedTest2("CrossFileRename_SecondProject.asmdef"); }
        [Test] public void TestRenameFile() { DoNamedTest2(); }

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

#if RESHARPER
        protected override TestSolutionConfiguration CreateSolutionConfiguration(PlatformID platformID,
            ICollection<KeyValuePair<TargetFrameworkId, IEnumerable<string>>> referencedLibraries,
            IEnumerable<string> fileSet)
#else
        protected override TestSolutionConfiguration CreateSolutionConfiguration(
            ICollection<KeyValuePair<Util.Dotnet.TargetFrameworkIds.TargetFrameworkId, IEnumerable<string>>> referencedLibraries,
            IEnumerable<string> fileSet)
#endif
        {
            if (fileSet == null)
                throw new ArgumentNullException(nameof(fileSet));

            var mainProjectFileSet = fileSet.Where(filename => !filename.Contains("_SecondProject"));
            var mainAbsoluteFileSet = mainProjectFileSet.Select(path => TestDataPath2.Combine(path)).ToList();

            var descriptors =
                new Dictionary<IProjectDescriptor, IList<Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>>>();

            var mainDescriptorPair = CreateProjectDescriptor(
#if RESHARPER
                platformID,
#endif
                ProjectName, ProjectName, mainAbsoluteFileSet,
                referencedLibraries, ProjectGuid);
            descriptors.Add(mainDescriptorPair.First, mainDescriptorPair.Second);

            var referencedProjectFileSet = fileSet.Where(filename => filename.Contains("_SecondProject")).ToList();
            if (Enumerable.Any(referencedProjectFileSet))
            {
                var secondAbsoluteFileSet =
                    referencedProjectFileSet.Select(path => TestDataPath2.Combine(path)).ToList();
                var secondProjectName = "Second_" + ProjectName;
                var secondDescriptorPair = CreateProjectDescriptor(
#if RESHARPER
                    platformID,
#endif
                    secondProjectName, secondProjectName,
                    secondAbsoluteFileSet, referencedLibraries, SecondProjectGuid);
                descriptors.Add(secondDescriptorPair.First, secondDescriptorPair.Second);
            }

            return new TestSolutionConfiguration(SolutionFileName, descriptors);
        }
    }
}