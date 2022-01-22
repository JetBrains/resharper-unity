using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Caches;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.HlslSupport
{
    [SolutionComponent]
    public class ShaderContextCache : IPreferredRootFileProvider
    {
        private readonly ISolution mySolution;
        private readonly InjectedHlslFileLocationTracker myLocationTracker;
        private readonly DocumentManager myManager;
        private readonly ILogger myLogger;
        private readonly DirectMappedCache<VirtualFileSystemPath, IRangeMarker> myShaderContext = new(100);

        public ShaderContextCache(ISolution solution, InjectedHlslFileLocationTracker locationTracker, DocumentManager manager, ILogger logger)
        {
            mySolution = solution;
            myLocationTracker = locationTracker;
            myManager = manager;
            myLogger = logger;
        }

        public void SetContext(IPsiSourceFile psiSourceFile, CppFileLocation? root)
        {
            using (ReadLockCookie.Create())
            {
                if (root.HasValue)
                {
                    Assertion.Assert(root.Value.RootRange.IsValid, "root.RootRange.IsValid()");
                    var range = myManager.CreateRangeMarker(new DocumentRange(root.Value.GetDocument(mySolution),
                        root.Value.RootRange));
                    myShaderContext.AddToCache(psiSourceFile.GetLocation(), range);
                }
                else
                {
                    myShaderContext.RemoveFromCache(psiSourceFile.GetLocation());
                }
            }

            var solution = psiSourceFile.GetSolution();
            var psiFiles = solution.GetPsiServices().Files;

            psiFiles.InvalidatePsiFilesCache(psiSourceFile);
            solution.GetComponent<IDaemon>().Invalidate();
        }

        public CppFileLocation GetPreferredRootFile(CppFileLocation currentFile)
        {
            using (ReadLockCookie.Create())
            {
                var sourceFile = currentFile.GetRandomSourceFile(mySolution);

                if (myShaderContext.TryGetFromCache(sourceFile.GetLocation(), out var result))
                {
                    var path = myManager.TryGetProjectFile(result.Document)?.Location;
                    if (path != null)
                    {
                        var location =
                            new CppFileLocation(new FileSystemPathWithRange(path, result.DocumentRange.TextRange));
                        if (!myLocationTracker.Exists(location))
                        {
                            myLogger.Trace(
                                $"Reset context for {sourceFile.GetPersistentIdForLogging()}, because inject is not registered");
                            myShaderContext.RemoveFromCache(sourceFile.GetLocation());
                            return CppFileLocation.EMPTY;
                        }

                        return location;
                    }
                }
            }

            return CppFileLocation.EMPTY;
        }
    }
}