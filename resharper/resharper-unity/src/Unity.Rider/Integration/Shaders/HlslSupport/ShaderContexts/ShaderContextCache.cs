#nullable enable

using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Cpp.Caches;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.Caches;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderContexts
{
    [SolutionComponent]
    public class ShaderContextCache : IPreferredRootFileProvider, ICppChangeProvider
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly ChangeManager myChangeManager;
        private readonly InjectedHlslFileLocationTracker myLocationTracker;
        private readonly DocumentManager myManager;
        private readonly ILogger myLogger;
        private readonly DirectMappedCache<VirtualFileSystemPath, IRangeMarker> myShaderContext = new(100);

        public ShaderContextCache(Lifetime lifetime, ISolution solution, ChangeManager changeManager, InjectedHlslFileLocationTracker locationTracker, DocumentManager manager, ILogger logger)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myChangeManager = changeManager;
            myLocationTracker = locationTracker;
            myManager = manager;
            myLogger = logger;
            
            myChangeManager.RegisterChangeProvider(lifetime, this);
        }

        public void SetContext(IPsiSourceFile psiSourceFile, IRangeMarker? range)
        {
            if (range != null)
                myShaderContext.AddToCache(psiSourceFile.GetLocation(), range);
            else
                myShaderContext.RemoveFromCache(psiSourceFile.GetLocation());

            mySolution.Locks.ExecuteOrQueueWithWriteLockWhenAvailableEx(myLifetime, $"Updating shader context for {psiSourceFile}", () =>
            {
                if (psiSourceFile.IsValid())
                    myChangeManager.OnProviderChanged(this, new ShaderContextChange(psiSourceFile), SimpleTaskExecutor.Instance);
            }).NoAwait();
        }

        public CppFileLocation GetPreferredRootFile(CppFileLocation currentFile)
        {
            if (currentFile.IsInjected())
                return currentFile;
            
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

        public object? Execute(IChangeMap changeMap) => null;
        
        private class ShaderContextChange : ICppChange
        {
            private readonly IPsiSourceFile mySourceFile;

            public ShaderContextChange(IPsiSourceFile sourceFile) => mySourceFile = sourceFile;

            public bool InvalidateProperties => false;
            public IReadOnlyCollection<(IProject Project, CppProjectChangeFlags ChangeFlags)> GetProjectChanges(ISolution solution) => EmptyList<(IProject Project, CppProjectChangeFlags ChangeFlags)>.Instance;

            public IReadOnlyCollection<CppSourceFile> ChangedFiles => FixedList.Of(new CppSourceFile(mySourceFile));
        }
    }
}