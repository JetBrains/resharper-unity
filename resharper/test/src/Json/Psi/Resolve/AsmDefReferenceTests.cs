using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel.Update;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Psi.Resolve
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferenceTests : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"Json\Psi\Resolve";
        protected override bool AcceptReference(IReference reference) => reference is AsmDefNameReference;

        [Test] public void TestUnresolvedReference01() { DoNamedTest2(); }
        [Test] public void TestUnresolvedReference02() { DoNamedTest2("UnresolvedReference02_SecondProject.asmdef"); }
        [Test] public void TestCrossProjectReference() { DoNamedTest2("CrossProjectReference_SecondProject.asmdef");}
        [Test] public void TestCorrectJsonReferences() { DoNamedTest2(); }

        protected override TestSolutionConfiguration CreateSolutionConfiguration(
            ICollection<KeyValuePair<TargetFrameworkId, IEnumerable<string>>> referencedLibraries,
            IEnumerable<string> fileSet)
        {
            if (fileSet == null)
                throw new ArgumentNullException(nameof(fileSet));

            var mainProjectFileSet = fileSet.Where(filename => !filename.Contains("_SecondProject"));
            var mainAbsoluteFileSet = mainProjectFileSet.Select(path => TestDataPath2.Combine(path)).ToList();

            var descriptors =
                new Dictionary<IProjectDescriptor, IList<Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>>>();

            var mainDescriptorPair = CreateProjectDescriptor(ProjectName, ProjectName, mainAbsoluteFileSet,
                referencedLibraries, ProjectGuid);
            descriptors.Add(mainDescriptorPair.First, mainDescriptorPair.Second);

            var referencedProjectFileSet = fileSet.Where(filename => filename.Contains("_SecondProject")).ToList();
            if (referencedProjectFileSet.Any())
            {
                var secondAbsoluteFileSet =
                    referencedProjectFileSet.Select(path => TestDataPath2.Combine(path)).ToList();
                var secondProjectName = "Second_" + ProjectName;
                var secondDescriptorPair = CreateProjectDescriptor(secondProjectName, secondProjectName,
                    secondAbsoluteFileSet, referencedLibraries, SecondProjectGuid);
                descriptors.Add(secondDescriptorPair.First, secondDescriptorPair.Second);
            }

            return new TestSolutionConfiguration(SolutionFileName, descriptors);
        }
    }
}