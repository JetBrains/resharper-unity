using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel.Update;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;
using PlatformID = JetBrains.Application.platforms.PlatformID;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Feature.Services.CodeCompletion
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencesCompletionListTests : TwoProjectCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;
        protected override string RelativeTestDataPath => @"Json\CodeCompletion\AsmDefReferences";

        [Test] public void TestList01() { DoNamedTest("Ref01_SecondProject.asmdef"); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencesCompletionActionTests : TwoProjectCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
        protected override string RelativeTestDataPath => @"Json\CodeCompletion\AsmDefReferences";
        protected override bool CheckAutomaticCompletionDefault() => true;
        protected override LookupListSorting Sorting => LookupListSorting.ByRelevance;

        [Test] public void TestAction01() { DoNamedTest("Ref01_SecondProject.asmdef"); }
    }

    public abstract class TwoProjectCodeCompletionTestBase : CodeCompletionTestBase
    {
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
