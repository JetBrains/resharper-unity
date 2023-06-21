#nullable enable
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel
{
    public interface IProjectFileTypeUserDataProvider
    {
        void AddUserData(ReadonlyUserDataPerSubjectBuilder<ProjectFileType> builder);
    }
}