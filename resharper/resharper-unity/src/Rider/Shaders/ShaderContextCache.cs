using JetBrains.Application.PersistentMap;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Shaders
{
    [SolutionComponent]
    public class ShaderContextCache
    {
        private readonly ISolution mySolution;
        private readonly IOptimizedPersistentSortedMap<IPsiSourceFile, CppFileLocation> myShaderContext;
    
        public ShaderContextCache(Lifetime lifetime, ISolution solution, IPersistentIndexManager persistentIndexManager)
        {
            mySolution = solution;
            myShaderContext = persistentIndexManager.GetPersistentMap(lifetime, "ShaderContextCache", CppFileLocationUnsafeMarshaller.Instance);
        }


        public void SetContext(IPsiSourceFile psiSourceFile, CppFileLocation root)
        {
            myShaderContext[psiSourceFile] = root;

            var solution = psiSourceFile.GetSolution();
            var psiFiles = solution.GetPsiServices().Files;
            psiFiles.InvalidatePsiFilesCache(psiSourceFile);
            solution.GetComponent<IDaemon>().Invalidate();
        }

        public CppFileLocation GetCustomRootFor(CppFileLocation currentFile)
        {
            if (myShaderContext.TryGetValue(currentFile.GetRandomSourceFile(mySolution), out var result))
                return result;
            return CppFileLocation.EMPTY;
        }
    }
}