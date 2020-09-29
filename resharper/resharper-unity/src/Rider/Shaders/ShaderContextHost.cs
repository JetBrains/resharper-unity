using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Documents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Shaders
{
    [SolutionComponent]
    public class ShaderContextHost
    {
        private readonly ISolution mySolution;
        private readonly IPsiFiles myPsiFiles;
        private readonly CppGlobalSymbolCache myCppGlobalSymbolCache;
        private readonly DocumentHost myDocumentHost;
        private readonly ShaderContextCache myShaderContextCache;

        public ShaderContextHost(Lifetime lifetime, ISolution solution,UnityHost unityHost, IPsiFiles psiFiles, CppGlobalSymbolCache cppGlobalSymbolCache,
            DocumentHost documentHost, ShaderContextCache shaderContextCache)
        {
            mySolution = solution;
            myPsiFiles = psiFiles;
            myCppGlobalSymbolCache = cppGlobalSymbolCache;
            myDocumentHost = documentHost;
            myShaderContextCache = shaderContextCache;

            unityHost.PerformModelAction(t =>
            {
                t.RequestShaderContexts.Set((lt, id) =>
                {
                    var sourceFile = GetSourceFile(id);
                    if (sourceFile == null)
                        return Rd.Tasks.RdTask<List<ShaderContextDataBase>>.Successful(new List<ShaderContextDataBase>());
                    var task = new Rd.Tasks.RdTask<List<ShaderContextDataBase>>();
                    RequestShaderContexts(lt, sourceFile, task);

                    return task;
                });

                t.ChangeContext.Advise(lifetime, c =>
                {
                    IPsiSourceFile sourceFile = GetSourceFile(c.Target);
                    if (sourceFile == null)
                        return;
                    
                    var cppFileLocation = new CppFileLocation(new FileSystemPathWithRange(FileSystemPath.Parse(c.Path), new TextRange(c.Start, c.End)));
                    shaderContextCache.SetContext(sourceFile, cppFileLocation);
                });
                
                t.RequestCurrentContext.Set((lt, id) =>
                {
                    var sourceFile = GetSourceFile(id);
                    if (sourceFile == null)
                        return Rd.Tasks.RdTask<ShaderContextDataBase>.Successful(new AutoShaderContextData());
                    
                    var task = new Rd.Tasks.RdTask<ShaderContextDataBase>();
                    RequestCurrentContext(lt, sourceFile, task);
                    return task;

                });
            });
        }


        private IPsiSourceFile GetSourceFile(EditableEntityId id)
        {
            using (ReadLockCookie.Create())
            {
                var document = myDocumentHost.TryGetHostDocument(id);
                return document?.GetPsiSourceFile(mySolution);
            }
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
                    var possibleRoots = myCppGlobalSymbolCache.IncludesGraphCache.CollectPossibleRoots(new CppFileLocation(sourceFile));
                    if (possibleRoots.Contains(currentRoot))
                    {
                        mySolution.Locks.ExecuteOrQueueEx(lt, "SetCurrentContext", () =>
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

            using (ReadLockCookie.Create())
            {
                myPsiFiles.CommitAllDocumentsAsync(() =>
                {
                    var possibleRoots = myCppGlobalSymbolCache.IncludesGraphCache.CollectPossibleRoots(new CppFileLocation(sourceFile));
                    var result = new List<ShaderContextDataBase>();
                    foreach (var root in possibleRoots)
                    {
                        if (root.IsInjected())
                            result.Add(GetContextDataFor(root));
                    }
                    task.Set(result);
                }, () => RequestShaderContexts(lt, sourceFile, task));
            }
        }

        private ShaderContextData GetContextDataFor(CppFileLocation root)
        {
            var folderFullPath = root.Location.Parent;
            var folderPath = folderFullPath.TryMakeRelativeTo(mySolution.SolutionDirectory);

            var folderHint = folderPath.FullPath.ShortenTextWithEllipsis(40);
            return new ShaderContextData(root.Location.FullPath, root.Location.Name, folderHint, root.RootRange.StartOffset, root.RootRange.EndOffset);
        }
    }
}