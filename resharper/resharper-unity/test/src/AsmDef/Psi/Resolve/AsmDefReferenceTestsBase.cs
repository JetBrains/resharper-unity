using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel.Update;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Psi.Resolve
{
    public abstract class AsmDefReferenceTestsBase<T> : ReferenceTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Psi\Resolve";
        protected override bool AcceptReference(IReference reference) => reference is T;


        protected override TestSolutionConfiguration CreateSolutionConfiguration(
            ICollection<KeyValuePair<TargetFrameworkId, IEnumerable<string>>> referencedLibraries,
            IEnumerable<string> fileSet)
        {
            if (fileSet == null)
                throw new ArgumentNullException(nameof(fileSet));

            var mainProjectFileSet = fileSet.Where(filename => !filename.Contains("_SecondProject"));
            var mainAbsoluteFileSet = mainProjectFileSet.Select(path => TestDataPath.Combine(path)).ToList();

            var descriptors =
                new Dictionary<IProjectDescriptor, IList<Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>>>();

            var mainDescriptorPair = CreateProjectDescriptor(ProjectName, ProjectName, mainAbsoluteFileSet,
                referencedLibraries, ProjectGuid);
            descriptors.Add(mainDescriptorPair.First, mainDescriptorPair.Second);

            var referencedProjectFileSet = fileSet.Where(filename => filename.Contains("_SecondProject")).ToList();
            if (Enumerable.Any(referencedProjectFileSet))
            {
                var secondAbsoluteFileSet =
                    referencedProjectFileSet.Select(path => TestDataPath.Combine(path)).ToList();
                var secondProjectName = "Second_" + ProjectName;
                var secondDescriptorPair = CreateProjectDescriptor(secondProjectName, secondProjectName,
                    secondAbsoluteFileSet, referencedLibraries, SecondProjectGuid);
                descriptors.Add(secondDescriptorPair.First, secondDescriptorPair.Second);
            }

            return new TestSolutionConfiguration(FileSystemPath.Empty, descriptors);
        }
    }
}