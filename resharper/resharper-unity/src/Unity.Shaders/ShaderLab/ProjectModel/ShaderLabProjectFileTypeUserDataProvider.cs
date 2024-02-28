#nullable enable
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class ShaderLabProjectFileTypeUserDataProvider : IProjectFileTypeUserDataProvider
    {
        public void AddUserData(ReadonlyUserDataPerSubjectBuilder<ProjectFileType> builder)
        {
            if (ShaderLabProjectFileType.Instance is {} projectFileType)
                builder.Add(projectFileType, UnityExternalProjectFileTypes.ExternalModuleFileFlagsKey, ExternalModuleFileFlags.IndexAlways | ExternalModuleFileFlags.TreatAsNonGenerated);
        }
    }
}
