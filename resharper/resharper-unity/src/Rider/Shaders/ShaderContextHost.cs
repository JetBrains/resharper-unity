using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Shaders
{
    [SolutionComponent]
    public class ShaderContextHost
    {
        private readonly ISolution mySolution;
        private readonly IPsiFiles myPsiFiles;
        private readonly CppGlobalSymbolCache myCppGlobalSymbolCache;
        private readonly DocumentHostBase myDocumentHost;
        private readonly ShaderContextCache myShaderContextCache;
        private readonly ShaderContextDataPresentationCache myShaderContextDataPresentationCache;

        public ShaderContextHost(Lifetime lifetime, ISolution solution, IPsiFiles psiFiles,
                                 CppGlobalSymbolCache cppGlobalSymbolCache,
                                 ShaderContextCache shaderContextCache,
                                 ShaderContextDataPresentationCache shaderContextDataPresentationCache, ILogger logger,
                                 [CanBeNull] FrontendBackendHost frontendBackendHost = null)
        {
            mySolution = solution;
            myPsiFiles = psiFiles;
            myCppGlobalSymbolCache = cppGlobalSymbolCache;
            myDocumentHost = DocumentHostBase.GetInstance(solution);
            myShaderContextCache = shaderContextCache;
            myShaderContextDataPresentationCache = shaderContextDataPresentationCache;

            if (frontendBackendHost == null || myDocumentHost == null)
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
                            return Rd.Tasks.RdTask<List<ShaderContextDataBase>>.Successful(
                                new List<ShaderContextDataBase>());
                        var task = new Rd.Tasks.RdTask<List<ShaderContextDataBase>>();
                        RequestShaderContexts(lt, sourceFile, task);

                        return task;
                    }
                });

                t.ChangeContext.Advise(lifetime, c =>
                {
                    logger.Verbose("Setting new shader context for file");
                    using (ReadLockCookie.Create())
                    {
                        IPsiSourceFile sourceFile = GetSourceFile(c.Target);
                        if (sourceFile == null)
                            return;

                        var cppFileLocation = new CppFileLocation(
                            new FileSystemPathWithRange(FileSystemPath.Parse(c.Path), new TextRange(c.Start, c.End)));
                        shaderContextCache.SetContext(sourceFile, cppFileLocation);
                    }
                });

                t.SetAutoShaderContext.Advise(lifetime, id =>
                {
                    using (ReadLockCookie.Create())
                    {
                        IPsiSourceFile sourceFile = GetSourceFile(id);
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
                            return Rd.Tasks.RdTask<ShaderContextDataBase>.Successful(new AutoShaderContextData());

                        var task = new Rd.Tasks.RdTask<ShaderContextDataBase>();
                        RequestCurrentContext(lt, sourceFile, task);
                        return task;
                    }
                });
            });
        }


        private IPsiSourceFile GetSourceFile(RdDocumentId id)
        {
            var document = myDocumentHost.TryGetHostDocument(id);
            return document?.GetPsiSourceFile(mySolution);
        }

        private void RequestCurrentContext(Lifetime lt, IPsiSourceFile sourceFile, Rd.Tasks.RdTask<ShaderContextDataBase> task)
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
                            task.Set(GetContextDataFor(currentRoot));
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

        private void RequestShaderContexts(Lifetime lt, IPsiSourceFile sourceFile, Rd.Tasks.RdTask<List<ShaderContextDataBase>> task)
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

        private ShaderContextData GetContextDataFor(CppFileLocation root)
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