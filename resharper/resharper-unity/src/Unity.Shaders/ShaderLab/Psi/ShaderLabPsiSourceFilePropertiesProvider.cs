#nullable enable
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.ReSharper.Psi.PerformanceThreshold;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi
{
    [PsiSharedComponent]
    public class ShaderLabPsiSourceFilePropertiesProvider : IPsiSourceFilePropertiesProvider
    {
        public IPsiSourceFileProperties? GetPsiProperties(IPsiSourceFileProperties? prevProperties, IProject project,
            IProjectFile? projectFile, IPsiSourceFile sourceFile)
        {
            using (ReadLockCookie.Create())
            {
                if (sourceFile.LanguageType.Is<ShaderLabProjectFileType>())
                {
                    if (ShouldBeExcluded(prevProperties, project, sourceFile))
                        return ExcludedProjectPsiSourceFilePropertiesProvider.ExcludedProjectPsiSourceFileProperties.Instance;
                    if (project.GetSolution().TryGetComponent<PackageManager>() is {} packageManager && packageManager.IsLocalPackageCacheFile(sourceFile.GetLocation()))
                        return ShaderFilesProperties.ShaderLabPackageLocalCacheFileProperties;
                }
            }

            return prevProperties;
        }

        // Visual C++ creates a hidden project to contain files for "single file intellisense".
        // https://blogs.msdn.microsoft.com/vcblog/2015/04/29/single-file-intellisense-and-other-ide-improvements-in-vs2015/
        // For some reason, it will add open .shader files, although we don't get intellisense.
        // I presume this is some kind of DirectX format (Cg/HLSL/whatever). ReSharper creates
        // a PSI module for this project, complete with PSI source files for the .shader files
        // that already exist in the normal C# project. This means we parse them multiple times
        // and causes issues with resolve - find usages on a ShaderLab property now has two
        // targets, one from the C# project and another from the hidden VC++ project.
        // This provider will provide new PSI properties for the hidden .shader files, so that
        // we don't add them into the PSI code model.
        private bool ShouldBeExcluded(IPsiSourceFileProperties? prevProperties, IProject project, IPsiSourceFile sourceFile) => 
            sourceFile is not IExternalPsiSourceFile
            && prevProperties is { ShouldBuildPsi: true, ProvidesCodeModel: true } 
            // R# already has a helper method to recognise the SFI project - IsVCXMiscProjectInVs2015
            && project.IsVCXMiscProjectInVs2015();

        public double Order => int.MaxValue;
    }
}