using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class ShaderlabCppInitialFilesProvider : ICppInitialFilesProvider
    {
        private readonly PsiModules myPsiModules;

        public ShaderlabCppInitialFilesProvider(PsiModules psiModules)
        {
            myPsiModules = psiModules;
        }

        public IEnumerable<CppFileLocation> GetCppFileLocations(SeldomInterruptCheckerWithCheckTime checker)
        {
            foreach (var module in myPsiModules.GetSourceModules())
            {
                if (module.ContainingProjectModule is IProject project && project.IsVCXMiscProjectInVs2015())
                    continue;

                foreach (var f in module.SourceFiles)
                {
                    checker?.CheckForInterrupt();

                    if (f.IsValid() && f.LanguageType.Is<ShaderLabProjectFileType>())
                    {
                        foreach (var cppFileLocation in ShaderLabCppHelper.GetCppFileLocations(f))
                        {
                            yield return cppFileLocation.Location;
                        }
                    }
                }
            }
        }
    }
}