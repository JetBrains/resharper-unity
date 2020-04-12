using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.ReSharper.Psi.PerformanceThreshold;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi
{
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
    [PsiSharedComponent]
    public class SingleFileIntellisensePsiPropertiesProvider : IPsiSourceFilePropertiesProvider
    {
        public IPsiSourceFileProperties GetPsiProperties(IPsiSourceFileProperties prevProperties, IProject project,
            IProjectFile projectFile, IPsiSourceFile sourceFile)
        {
            using (ReadLockCookie.Create())
            {
                // R# already has a helper method to recognise the SFI project - IsVCXMiscProjectInVs2015
                if (prevProperties != null && prevProperties.ShouldBuildPsi
                                           && prevProperties.ProvidesCodeModel
                                           && !(sourceFile is IExternalPsiSourceFile)
                                           && project.IsVCXMiscProjectInVs2015()
                                           && projectFile.LanguageType.Is<ShaderLabProjectFileType>())
                {
                    return ExcludedProjectPsiSourceFilePropertiesProvider.ExcludedProjectPsiSourceFileProperties
                        .Instance;
                }
            }

            return prevProperties;
        }

        public double Order => int.MaxValue;
    }
}