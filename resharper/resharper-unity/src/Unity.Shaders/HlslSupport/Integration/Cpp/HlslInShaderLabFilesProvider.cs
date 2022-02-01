using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class InjectedHlslInitialFilesProvider : ICppInitialFilesProvider
    {
        private readonly PsiModules myPsiModules;

        public InjectedHlslInitialFilesProvider(PsiModules psiModules)
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
                        foreach (var cppFileLocation in InjectedHlslLocationHelper.GetCppFileLocations(f))
                        {
                            yield return cppFileLocation.Location;
                        }
                    }
                }
            }
        }
    }
}