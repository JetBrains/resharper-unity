using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel.Update;
using JetBrains.ReSharper.Daemon.JavaScript.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;
#if RESHARPER
using PlatformID = JetBrains.Application.platforms.PlatformID;
#endif

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefDuplicateItemsProblemAnalyzerTests : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonLanguage.Instance;

        protected override string RelativeTestDataPath => @"Json\Daemon\Stages\Analysis\";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is JsonValidationFailedWarning;
        }

        // TODO: ReSharper will run element problem analyzers twice for JSON files
        // Which means we get multiple highlights. Not a huge deal in practice, but if this
        // test suddenly starts to fail, that might be why. See RSRP-467138
        [Test] public void Test01() { DoNamedTest("Test01_SecondProject.asmdef"); }

        // If we don't have valid (but duplicated references), the invalid reference error trumps the duplicate item warning
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