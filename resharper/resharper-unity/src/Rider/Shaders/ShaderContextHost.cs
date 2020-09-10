using System.Collections;
using System.Collections.Generic;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Host.Features.Documents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.Util;
using StringUtil = JetBrains.Util.StringUtil;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Shaders
{
    [SolutionComponent]
    public class ShaderContextHost
    {
        private readonly ISolution mySolution;
        private readonly IPsiFiles myPsiFiles;
        private readonly CppGlobalSymbolCache myCppGlobalSymbolCache;
        private readonly DocumentHost myDocumentHost;

        public ShaderContextHost(Lifetime lifetime, ISolution solution,UnityHost unityHost, IPsiFiles psiFiles, CppGlobalSymbolCache cppGlobalSymbolCache,
            DocumentHost documentHost, ShaderContextCache shaderContextCache)
        {
            mySolution = solution;
            myPsiFiles = psiFiles;
            myCppGlobalSymbolCache = cppGlobalSymbolCache;
            myDocumentHost = documentHost;
            
            unityHost.PerformModelAction(t =>
            {
                t.RequestShaderContexts.Set((lt, id) =>
                {
                    using (ReadLockCookie.Create())
                    {
                        var task = new Rd.Tasks.RdTask<List<ShaderContextData>>();
                        var document = myDocumentHost.TryGetHostDocument(id);
                        var sourceFile = document?.GetPsiSourceFile(mySolution);
                        if (sourceFile == null) return null;

                        RequestShaderContexts(lt, sourceFile, task);

                        return task;
                    }
                });

                t.ChangeContext.Advise(lifetime, c =>
                {
                    IPsiSourceFile sourceFile;
                    using (ReadLockCookie.Create())
                    {
                        var document = myDocumentHost.TryGetHostDocument(c.Target);
                        sourceFile = document?.GetPsiSourceFile(mySolution);
                        if (sourceFile == null) return;
                    }
                    
                    var cppFileLocation = new CppFileLocation(new FileSystemPathWithRange(FileSystemPath.Parse(c.Path), new TextRange(c.Start, c.End)));
                    shaderContextCache.SetContext(sourceFile, cppFileLocation);
                });
            });
        }

        private void RequestShaderContexts(Lifetime lt, IPsiSourceFile sourceFile, Rd.Tasks.RdTask<List<ShaderContextData>> task)
        {
            if (!lt.IsAlive)
            {
                task.SetCancelled();
                return;
                
            }
            myPsiFiles.CommitAllDocumentsAsync(() =>
            {
                var possibleRoots = myCppGlobalSymbolCache.IncludesGraphCache.CollectPossibleRoots(new CppFileLocation(sourceFile));
                var result = new List<ShaderContextData>();
                foreach (var root in possibleRoots)
                {
                    var folderFullPath = root.Location.Parent;
                    var folderPath = folderFullPath.TryMakeRelativeTo(mySolution.SolutionDirectory) ?? folderFullPath;

                    var folderHint = folderPath.FullPath.ShortenTextWithEllipsis(40);
                    var element = new ShaderContextData(root.Location.FullPath, root.Location.Name, folderHint, root.RootRange.StartOffset, root.RootRange.EndOffset);
                    result.Add(element);
                }
                task.Set(result);
            }, () => RequestShaderContexts(lt, sourceFile, task));
        }
    }
}