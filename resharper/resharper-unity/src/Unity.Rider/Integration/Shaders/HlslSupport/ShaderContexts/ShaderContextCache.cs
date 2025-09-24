#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderContexts;

[SolutionComponent(InstantiationEx.LegacyDefault)]
public class ShaderContextCache : IPreferredRootFileProvider, ICppChangeProvider
{
    private readonly Lifetime myLifetime;
    private readonly ISolution mySolution;
    private readonly CppGlobalSymbolCache myGlobalSymbolCache;
    private readonly ChangeManager myChangeManager;
    private readonly DocumentManager myManager;
    private readonly ILogger myLogger;
    private readonly DirectMappedCache<VirtualFileSystemPath, IRangeMarker> myShaderContext = new(100);

    public ShaderContextCache(Lifetime lifetime, ISolution solution, ChangeManager changeManager, DocumentManager manager, ILogger logger, CppGlobalSymbolCache globalSymbolCache)
    {
        myLifetime = lifetime;
        mySolution = solution;
        myChangeManager = changeManager;
        myManager = manager;
        myLogger = logger;
        myGlobalSymbolCache = globalSymbolCache;

        myChangeManager.RegisterChangeProvider(lifetime, this);
    }

    public void SetContext(IPsiSourceFile psiSourceFile, IRangeMarker? range)
    {
        if (range != null)
            myShaderContext.AddToCache(psiSourceFile.GetLocation(), range);
        else
            myShaderContext.RemoveFromCache(psiSourceFile.GetLocation());

        mySolution.Locks.ExecuteOrQueueWriteLockAsync(myLifetime, $"Updating shader context for {psiSourceFile}", () =>
        {
            if (psiSourceFile.IsValid())
                myChangeManager.OnProviderChanged(this, new CppChange(new CppFileLocation(psiSourceFile)), SimpleTaskExecutor.Instance);
        }).NoAwait();
    }

    private static bool IsPreferredRoot(CppFileLocation file) => file.IsInjected() || UnityShaderFileUtils.IsComputeShaderFile(file.Location);

    public CppFileLocation GetAssignedRoot(CppFileLocation currentFile)
    {
        mySolution.Locks.AssertReadAccessAllowed();
            
        return GetAssignedRoot(currentFile, myGlobalSymbolCache.IncludesGraphCache.CollectPossibleRootsForFile(currentFile));
    }

    private CppFileLocation GetCppFileLocation(IRangeMarker rangeMarker)
    {
        if (!rangeMarker.IsValid || myManager.TryGetProjectFile(rangeMarker.Document) is not { Location: var rootPath }) 
            return CppFileLocation.EMPTY;
            
        var range = rangeMarker.DocumentRange.TextRange;
        return range.IsEmpty ? new CppFileLocation(rootPath) : new CppFileLocation(new FileSystemPathWithRange(rootPath, range));
    }
        
    private CppFileLocation GetAssignedRoot(CppFileLocation currentFile, IEnumerable<CppFileLocation> possibleRoots)
    {
        mySolution.Locks.AssertReadAccessAllowed();
            
        var sourceFile = currentFile.GetRandomSourceFile(myGlobalSymbolCache.CppModule);
        var path = sourceFile.GetLocation();
        if (!myShaderContext.TryGetFromCache(path, out var result))
            return CppFileLocation.EMPTY;

        var location = GetCppFileLocation(result);
        if (location.IsValid() && possibleRoots.Contains(location))
            return location;
            
        myLogger.Trace($"Reset context for {sourceFile.GetPersistentIdForLogging()}, because inject is not registered");
        myShaderContext.RemoveFromCache(path);
        return CppFileLocation.EMPTY;
    }

    public CppFileLocation GetPreferredRootFile(CppFileLocation currentFile)
    {
        if (IsPreferredRoot(currentFile))
            return currentFile;
            
        using (ReadLockCookie.Create())
        {
            var possibleRoots = myGlobalSymbolCache.IncludesGraphCache.CollectPossibleRootsForFile(currentFile).AsReadOnlyCollection();
            var assignedRoot = GetAssignedRoot(currentFile, possibleRoots);
            if (assignedRoot.IsValid())
                return assignedRoot;
            
            foreach (var root in possibleRoots)
            {
                if (root.IsValid() && IsPreferredRoot(root) && root.GetRandomSourceFile(myGlobalSymbolCache.CppModule) != null)
                    return root;
            }   

            return CppFileLocation.EMPTY;
        }
    }

    public object? Execute(IChangeMap changeMap) => null;
}