#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Changes;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.RdBackend.Common.Features.TextControls;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Backend.Env;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport
{
    [SolutionComponent]
    [ZoneMarker(typeof(IRiderFeatureZone))]
    public class ShaderContextHost
    {
        private readonly AutoShaderContextData myAutoContext = new();
        private readonly ISolution mySolution;
        private readonly IPsiFiles myPsiFiles;
        private readonly CppGlobalSymbolCache myCppGlobalSymbolCache;
        private readonly DocumentManager myDocumentManager;
        private readonly IDocumentHost myDocumentHost;
        private readonly FrontendBackendHost? myFrontendBackendHost;
        private readonly ShaderContextCache myShaderContextCache;
        private readonly ShaderContextDataPresentationCache myShaderContextDataPresentationCache;
        private readonly Dictionary<IPsiSourceFile, TrackingInfo> myTrackedFiles = new();
        private readonly Dictionary<RdDocumentId, ShaderContext> myActiveContexts = new();

        public ShaderContextHost(Lifetime lifetime, ILogger logger, ISolution solution,
            IPsiFiles psiFiles,
            DocumentManager documentManager,
            IDocumentHost documentHost,
            ITextControlHost textControlHost,
            CppGlobalSymbolCache cppGlobalSymbolCache,
            ShaderContextCache shaderContextCache,
            ShaderContextDataPresentationCache shaderContextDataPresentationCache, FrontendBackendHost? frontendBackendHost = null)
        {
            mySolution = solution;
            myPsiFiles = psiFiles;
            myDocumentHost = documentHost;
            myCppGlobalSymbolCache = cppGlobalSymbolCache;
            myShaderContextCache = shaderContextCache;
            myShaderContextDataPresentationCache = shaderContextDataPresentationCache;
            myDocumentManager = documentManager;
            myFrontendBackendHost = frontendBackendHost;

            frontendBackendHost?.Do(model =>
            {
                myShaderContextDataPresentationCache.CacheUpdated.Advise(lifetime, _ => SyncTrackedRoots());
                textControlHost.ViewHostTextControls(lifetime, (textControlLifetime, textControlId, textControl) => OnTextControlAdded(textControlLifetime, model, textControlId, textControl));

                model.CreateSelectShaderContextInteraction.SetAsync((lt, id) =>
                {
                    logger.Verbose("Requesting all shader context for file");
                    using (ReadLockCookie.Create())
                    {
                        var sourceFile = GetSourceFile(id);
                        if (sourceFile == null)
                            return Task.FromResult(new SelectShaderContextDataInteraction(new List<ShaderContextData>()));
                        return CreateSelectShaderContextInteraction(lt, id, sourceFile);
                    }
                });
            });
        }

        private void OnTextControlAdded(Lifetime textControlLifetime, FrontendBackendModel model, TextControlId textControlId, ITextControl textControl)
        {
            // State modifications only allowed from main thread  
            mySolution.Locks.AssertMainThread();

            var sourceFile = textControl.Document.GetPsiSourceFile(mySolution);
            if (sourceFile == null || !PsiSourceFileUtil.IsHlslFile(sourceFile))
                return;

            var documentId = textControlId.DocumentId;
            if (!myActiveContexts.TryGetValue(documentId, out var context))
            {
                context = new ShaderContext(documentId, sourceFile);
                myActiveContexts.Add(context.Lifetime, documentId, context);
                QueryCurrentContextDataAsync(context).NoAwait();
            }
            else
                context.IncrementRefCount();

            textControlLifetime.OnTermination(context.DecrementRefCount);
        }

        private void UpdateRoot(ShaderContext context, IPsiSourceFile? rootFile, IRangeMarker? rootRangeMarker)
        {
            context.RootRangeMarker = rootRangeMarker;
            if (rootFile == context.RootFile)
                return;
            
            context.RootFile = rootFile;
            if (rootFile == null)
            {
                context.RootLifetimes.TerminateCurrent();
                return;
            }

            var rootLifetime = context.RootLifetimes.Next(); 
            if (!myTrackedFiles.TryGetValue(rootFile, out var trackingInfo))
            {
                trackingInfo = new TrackingInfo(rootFile.Document.LastModificationStamp);
                myTrackedFiles.Add(rootFile, trackingInfo);
            }
            trackingInfo.Contexts.Add(context);

            rootLifetime.OnTermination(() =>
            {
                var contexts = myTrackedFiles[rootFile].Contexts;
                contexts.Remove(context);
                if (contexts.Count == 0)
                    myTrackedFiles.Remove(rootFile);
            });
        }

        private void SyncTrackedRoots()
        {
            // State modifications only allowed from main thread  
            mySolution.Locks.AssertMainThread();

            using (ReadLockCookie.Create())
            {
                var invalidContexts = new LocalList<ShaderContext>(); 
                foreach (var (rootFile, trackingInfo) in myTrackedFiles)
                {
                    if (!rootFile.IsValid())
                    {
                        invalidContexts.AddRange(trackingInfo.Contexts);
                        continue;
                    }
                    
                    var modificationStamp = rootFile.Document.LastModificationStamp;
                    if (trackingInfo.SyncStamp < modificationStamp)
                    {
                        foreach (var context in trackingInfo.Contexts)
                        {
                            // terminate current shader data lifetime
                            context.ShaderDataLifetimes.TerminateCurrent();
                            
                            var rootRangeMarker = context.RootRangeMarker;
                            Assertion.Assert(context.RootFile == rootFile, "Tracked root mapped to wrong shader context");
                            Assertion.Assert(rootRangeMarker != null, "Invalid context state: RootRangeMarker is null with non-null root file");
                            var contextData = GetContextDataFor(rootFile, rootRangeMarker.Range);
                            if (contextData != null)
                                SyncToFrontend(context, contextData);
                            else
                                invalidContexts.Add(context);
                        }

                        trackingInfo.SyncStamp = modificationStamp;
                    }
                }

                // root range or root file may be removed, have to reset contexts for such cases
                foreach (var context in invalidContexts) 
                    SetContextRoot(context, null, null);
            }
        }

        private async Task QueryCurrentContextDataAsync(ShaderContext context)
        {
            var lifetime = context.ShaderDataLifetimes.Next();
            var (rootFile, rootRangeMarker, contextData) = await lifetime.StartBackgroundRead(() =>
            {
                var targetLocation = new CppFileLocation(context.SourceFile);
                if (!TryGetCurrentRoot(targetLocation, out var rootLocation))
                    return (null, null, myAutoContext);
                var rootFile = rootLocation.GetRandomSourceFile(mySolution);
                var rootRange = rootLocation.RootRange;
                var rootRangeMarker = myDocumentManager.CreateRangeMarker(new DocumentRange(rootFile.Document, rootRange));
                return (rootFile, rangeMarker: rootRangeMarker, (ShaderContextDataBase?)GetContextDataFor(rootFile, rootRange) ?? myAutoContext);
            });

            await lifetime.StartMainRead(() => UpdateContextData(context, rootFile, rootRangeMarker, contextData));
        }

        private void SetContextRoot(RdDocumentId targetDocumentId, IPsiSourceFile targetSourceFile, IPsiSourceFile? rootFile, IRangeMarker? rootRangeMarker)
        {
            Assertion.Assert(rootRangeMarker is null == rootFile is null, "SetContextRoot: rootFile and rootRangeMarker should be both null or both not null");
            if (myActiveContexts.TryGetValue(targetDocumentId, out var context))
            {
                Assertion.Assert(context.SourceFile == targetSourceFile, "SetContextRoot: context mapped to document doesn't match target source file");
                SetContextRoot(context, rootFile, rootRangeMarker);
            }
            else
                myShaderContextCache.SetContext(targetSourceFile, rootRangeMarker);
        }

        private void SetContextRoot(ShaderContext context, IPsiSourceFile? rootFile, IRangeMarker? rootRangeMarker)
        {
            Assertion.Assert(rootRangeMarker is null == rootFile is null, "SetContextRoot: rootFile and rootRangeMarker should be both null or both not null");
            
            // terminal current shader data lifetime
            context.ShaderDataLifetimes.TerminateCurrent();

            if (rootFile != null 
                && rootFile.IsValid() 
                && rootRangeMarker!.Range is { IsValid: true } rootRange 
                && GetContextDataFor(rootFile, rootRange) is {} contextData)
            {
                myShaderContextCache.SetContext(context.SourceFile, rootRangeMarker);
                UpdateContextData(context, rootFile, rootRangeMarker, contextData);
            }
            else
            {
                // Set auto-context if there no mapping or mapping invalid 
                myShaderContextCache.SetContext(context.SourceFile, null);
                UpdateContextData(context, null, null, myAutoContext);
            }
        }

        private void UpdateContextData(ShaderContext context, IPsiSourceFile? rootFile, IRangeMarker? rootRangeMarker, ShaderContextDataBase contextData)
        {
            // State modifications only allowed from main thread
            mySolution.Locks.AssertMainThread();
            
            UpdateRoot(context, rootFile, rootRangeMarker);
            SyncToFrontend(context, contextData);
        }

        private void SyncToFrontend(ShaderContext context, ShaderContextDataBase contextData) => myFrontendBackendHost?.Do(model =>
        {
            // Once synced to model.ShaderContexts it won't be removed as long as context exists. We don't use Add with lifetime, because it removes initially added pair, but we need to remove by key no matter of value.
            if (!model.ShaderContexts.ContainsKey(context.DocumentId))
                context.Lifetime.Bracket(() => model.ShaderContexts.Add(context.DocumentId, contextData), () => model.ShaderContexts.Remove(context.DocumentId));
            else
                model.ShaderContexts[context.DocumentId] = contextData;
        });

        private IPsiSourceFile? GetSourceFile(RdDocumentId id)
        {
            var document = myDocumentHost.TryGetDocument(id);
            return document?.GetPsiSourceFile(mySolution);
        }

        private bool TryGetCurrentRoot(CppFileLocation target, out CppFileLocation currentRoot)
        {
            currentRoot = myShaderContextCache.GetPreferredRootFile(target);
            if (!currentRoot.IsValid())
                return false;

            var possibleRoots = myCppGlobalSymbolCache.IncludesGraphCache.CollectPossibleRootsForFile(target);
            return possibleRoots.Contains(currentRoot);
        }

        private async Task<SelectShaderContextDataInteraction> CreateSelectShaderContextInteraction(Lifetime lt, RdDocumentId documentId, IPsiSourceFile sourceFile)
        {
            var items = new List<ShaderContextData>();
            var roots = new List<(IPsiSourceFile SourceFile, IRangeMarker RangeMarker)>();
            await myPsiFiles.CommitWithRetryBackgroundRead(lt, () =>
            {
                var possibleRoots = myCppGlobalSymbolCache.IncludesGraphCache.CollectPossibleRootsForFile(new CppFileLocation(sourceFile)).ToList();
                foreach (var root in possibleRoots)
                {
                    if (root.IsInjected())
                    {
                        var rootSourceFile = root.GetRandomSourceFile(mySolution);
                        var textRange = root.RootRange;
                        var item = GetContextDataFor(rootSourceFile, textRange);
                        if (item != null)
                        {
                            var rangeMarker = myDocumentManager.CreateRangeMarker(new DocumentRange(rootSourceFile.Document, textRange));
                            roots.Add((rootSourceFile, rangeMarker));
                            items.Add(item);
                        }
                    }
                }

                return items;
            });
            var interaction = new SelectShaderContextDataInteraction(items);
            interaction.SelectItem.Advise(lt, index =>
            {
                // Callback from RD should always be on main thread 
                mySolution.Locks.AssertMainThread();

                using (ReadLockCookie.Create())
                {
                    if (index >= 0)
                    {
                        var root = roots[index];
                        SetContextRoot(documentId, sourceFile, root.SourceFile, root.RangeMarker);
                    }
                    else
                        SetContextRoot(documentId, sourceFile, null, null);
                }
            });
            return interaction;
        }

        private ShaderContextData? GetContextDataFor(IPsiSourceFile rootFile, TextRange rootRange)
        {
            var range = myShaderContextDataPresentationCache.GetRangeForShaderProgram(rootFile, rootRange);
            if (!range.HasValue)
                return null;

            var location = rootFile.GetLocation();
            var folderFullPath = location.Parent;
            var folderPath = folderFullPath.TryMakeRelativeTo(mySolution.SolutionDirectory);

            var folderHint = folderPath.FullPath.ShortenTextWithEllipsis(40);
            return new ShaderContextData(location.FullPath, location.Name, folderHint, rootRange.StartOffset, rootRange.EndOffset, range.Value.startLine + 1);
        }

        private class TrackingInfo
        {
            public readonly List<ShaderContext> Contexts = new();
            
            public ModificationStamp SyncStamp;

            public TrackingInfo(ModificationStamp syncStamp) => SyncStamp = syncStamp;
        }
    }
}