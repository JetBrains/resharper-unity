#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface IProjectFileTypeUserDataProvider
    {
        void AddUserData(ReadonlyUserDataPerSubjectBuilder<ProjectFileType> builder);
    }
}