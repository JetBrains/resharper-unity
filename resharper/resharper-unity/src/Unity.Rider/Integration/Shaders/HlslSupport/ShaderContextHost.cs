#nullable enable

using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Backend.Env;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;
using RdTask = JetBrains.Rd.Tasks.RdTask;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport
{
    [SolutionComponent]
    [ZoneMarker(typeof(IRiderFeatureZone))]
    public class ShaderContextHost
    {
        private readonly ISolution mySolution;
        private readonly IPsiFiles myPsiFiles;
        private readonly CppGlobalSymbolCache myCppGlobalSymbolCache;
        private readonly IDocumentHost myDocumentHost;
        private readonly ShaderContextCache myShaderContextCache;
        private readonly ShaderContextDataPresentationCache myShaderContextDataPresentationCache;

        public ShaderContextHost(Lifetime lifetime, ILogger logger, ISolution solution,
                                 IPsiFiles psiFiles,
                                 IDocumentHost documentHost,
                                 CppGlobalSymbolCache cppGlobalSymbolCache,
                                 ShaderContextCache shaderContextCache,
                                 ShaderContextDataPresentationCache shaderContextDataPresentationCache,
                                 FrontendBackendHost? frontendBackendHost = null)
        {
            mySolution = solution;
            myPsiFiles = psiFiles;
            myDocumentHost = documentHost;
            myCppGlobalSymbolCache = cppGlobalSymbolCache;
            myShaderContextCache = shaderContextCache;
            myShaderContextDataPresentationCache = shaderContextDataPresentationCache;

            if (frontendBackendHost == null)
                return;

            frontendBackendHost.Do(t =>
            {
                t.RequestShaderContexts.Set((lt, id) =>
                {
                    logger.Verbose("Requesting all shader context for file");
                    using (ReadLockCookie.Create())
                    {
                        var sourceFile = GetSourceFile(id);
                        if (sourceFile == null)
                            return RdTask.Successful(new List<ShaderContextDataBase>());

                        var task = new RdTask<List<ShaderContextDataBase>>();
                        RequestShaderContexts(lt, sourceFile, task);

                        return task;
                    }
                });

                t.ChangeContext.Advise(lifetime, c =>
                {
                    logger.Verbose("Setting new shader context for file");
                    using (ReadLockCookie.Create())
                    {
                        IPsiSourceFile? sourceFile = GetSourceFile(c.Target);
                        if (sourceFile == null)
                            return;

                        var cppFileLocation = new CppFileLocation(
                            new FileSystemPathWithRange(VirtualFileSystemPath.Parse(c.Path, InteractionContext.SolutionContext), new TextRange(c.Start, c.End)));
                        shaderContextCache.SetContext(sourceFile, cppFileLocation);
                    }
                });

                t.SetAutoShaderContext.Advise(lifetime, id =>
                {
                    using (ReadLockCookie.Create())
                    {
                        IPsiSourceFile? sourceFile = GetSourceFile(id);
                        if (sourceFile == null)
                            return;
                        shaderContextCache.SetContext(sourceFile, null);

                    }
                });

                t.RequestCurrentContext.Set((lt, id) =>
                {
                    logger.Verbose("Setting current context for file");
                    using (ReadLockCookie.Create())
                    {
                        var sourceFile = GetSourceFile(id);
                        if (sourceFile == null)
                            return RdTask.Successful<ShaderContextDataBase>(new AutoShaderContextData());

                        var task = new RdTask<ShaderContextDataBase>();
                        RequestCurrentContext(lt, sourceFile, task);
                        return task;
                    }
                });
            });
        }


        private IPsiSourceFile? GetSourceFile(RdDocumentId id)
        {
            var document = myDocumentHost.TryGetDocument(id);
            return document?.GetPsiSourceFile(mySolution);
        }

        private void RequestCurrentContext(Lifetime lt, IPsiSourceFile sourceFile, RdTask<ShaderContextDataBase> task)
        {
            var currentRoot = myShaderContextCache.GetPreferredRootFile(new CppFileLocation(sourceFile));
            if (!currentRoot.IsValid())
            {
                task.Set(new AutoShaderContextData());
                return;
            }

            mySolution.Locks.Tasks.StartNew(lt, Scheduling.FreeThreaded, () =>
            {
                using (ReadLockCookie.Create())
                {
                    var possibleRoots = myCppGlobalSymbolCache.IncludesGraphCache.CollectPossibleRootsForFile(new CppFileLocation(sourceFile)).ToList();
                    if (possibleRoots.Contains(currentRoot))
                    {
                        mySolution.Locks.ExecuteOrQueueReadLockEx(lt, "SetCurrentContext", () =>
                        {
                            var shaderContextData = GetContextDataFor(currentRoot) ??
                                                    (ShaderContextDataBase) new AutoShaderContextData();
                            task.Set(shaderContextData);
                        });
                    }
                    else
                    {
                        mySolution.Locks.ExecuteOrQueueEx(lt, "SetCurrentContext", () =>
                        {
                            task.Set(new AutoShaderContextData());
                        });
                    }
                }
            });
        }

        private void RequestShaderContexts(Lifetime lt, IPsiSourceFile sourceFile, RdTask<List<ShaderContextDataBase>> task)
        {
            if (!lt.IsAlive)
            {
                task.SetCancelled();
                return;
            }

            myPsiFiles.CommitAllDocumentsAsync(() =>
            {
                var possibleRoots = myCppGlobalSymbolCache.IncludesGraphCache.CollectPossibleRootsForFile(new CppFileLocation(sourceFile)).ToList();
                var result = new List<ShaderContextDataBase>();
                foreach (var root in possibleRoots)
                {
                    if (root.IsInjected())
                    {
                        var item = GetContextDataFor(root);
                        if (item != null)
                            result.Add(item);
                    }
                }
                task.Set(result);
            }, () => RequestShaderContexts(lt, sourceFile, task));
        }

        private ShaderContextData? GetContextDataFor(CppFileLocation root)
        {
            var range = myShaderContextDataPresentationCache.GetRangeForShaderProgram(root.GetRandomSourceFile(mySolution), root.RootRange);
            if (!range.HasValue)
                return null;

            var folderFullPath = root.Location.Parent;
            var folderPath = folderFullPath.TryMakeRelativeTo(mySolution.SolutionDirectory);

            var folderHint = folderPath.FullPath.ShortenTextWithEllipsis(40);
            return new ShaderContextData(root.Location.FullPath, root.Location.Name, folderHint, root.RootRange.StartOffset, root.RootRange.EndOffset, range.Value.startLine + 1);
        }
    }
}
