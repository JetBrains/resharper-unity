#nullable enable
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel
{
    [ShellComponent(InstantiationEx.LegacyDefault)]
    public class UserDataPerProjectFileType
    {
        private readonly Dictionary<ProjectFileType, ReadonlyUserData> myUserDataByProjectFileType;

        public UserDataPerProjectFileType(IEnumerable<IProjectFileTypeUserDataProvider> propertiesProviders)
        {
            var builder = new ReadonlyUserDataPerSubjectBuilder<ProjectFileType>();
            foreach (var propertiesProvider in propertiesProviders) 
                propertiesProvider.AddUserData(builder);

            myUserDataByProjectFileType = builder.Build();
        }

        public bool TryGetUserData(ProjectFileType projectFileType, out ReadonlyUserData data) => myUserDataByProjectFileType.TryGetValue(projectFileType, out data);

        public Dictionary<ProjectFileType, ReadonlyUserData>.Enumerator GetEnumerator() => myUserDataByProjectFileType.GetEnumerator();
    }
}
