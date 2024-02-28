#nullable enable
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.InputActions.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityProjectFileTypeUserDataProvider : IProjectFileTypeUserDataProvider
    {
        public void AddUserData(ReadonlyUserDataPerSubjectBuilder<ProjectFileType> builder)
        {
            foreach (var projectFileType in new ProjectFileType?[] { InputActionsProjectFileType.Instance, AsmDefProjectFileType.Instance, AsmRefProjectFileType.Instance })
            {
                if (projectFileType != null)
                    builder.Add(projectFileType, UnityExternalProjectFileTypes.ExternalModuleFileFlagsKey,  ExternalModuleFileFlags.IndexAlways | ExternalModuleFileFlags.TreatAsNonGenerated);
            }

            foreach (var projectFileType in new ProjectFileType?[] { MetaProjectFileType.Instance, UnityYamlProjectFileType.Instance })
            {
                if (projectFileType != null)
                    builder.Add(projectFileType, UnityExternalProjectFileTypes.ExternalModuleFileFlagsKey, ExternalModuleFileFlags.IndexWhenAssetsEnabled);
            }
        }
    }
}
