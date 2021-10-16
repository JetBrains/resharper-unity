using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Core;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ExternalSources
{
    // Just because I always forget how these fit together: an IProjectPsiModuleHandler represents the IPsiModule for
    // each project, and maps project files to IPsiSourceFiles. DefaultProjectPsiModuleHandler is the handler for most
    // projects, while MiscFilesProjectProjectPsiModuleHandler handles the Misc Files project. This will defer to
    // one of several IMiscFilesProjectPsiModuleProvider implementations for various specific files, and fallback to a
    // simple implementation for others.
    // If a Misc File comes from external sources, such as directly from a PDB (or inferred via PDB for e.g. enum or
    // interfaces) it is handled by ExternalSourcesPsiModuleProvider, which creates a PSI source file. If we want to
    // contribute additional compile symbols, we would normally use IPsiSourceFilePropertiesProvider, but external
    // sources PSI source files are created with an instance of ExternalSourceFileProperties, so we have to use
    // IExternalSourcesDefinesProvider instead, which is actually easier.
    //
    // Note that there is an edge case: if the user opens a source file from a Unity package that cannot be inferred to
    // belong to a known PDB (e.g. a file that contains a bunch of enums) then it is not considered external source,
    // and we won't be asked to contributed symbols (but if the user opens a file that contains at least one type with
    // sequence points, it will be considered as external source). We do nothing in this case, and potentially use the
    // wrong highlighting for #define branches.
    // This class also won't be called for source files that are part of assembly definitions that aren't compiled, such
    // as tests
    [SolutionComponent]
    public class UnityPackageDefinesProvider : IExternalSourcesDefinesProvider
    {
        private readonly PreProcessingDirectiveCache myPreProcessingDirectiveCache;

        public UnityPackageDefinesProvider(PreProcessingDirectiveCache preProcessingDirectiveCache)
        {
            myPreProcessingDirectiveCache = preProcessingDirectiveCache;
        }

        public PreProcessingDirective[] GetPreProcessingDirectives(IPsiModule psiModule)
        {
            var solution = psiModule.GetSolution();
            if (solution.HasUnityReference() && psiModule is IAssemblyPsiModule assemblyPsiModule)
                return myPreProcessingDirectiveCache.GetPreProcessingDirectives(assemblyPsiModule.Assembly);
            return EmptyArray<PreProcessingDirective>.Instance;
        }
    }
}