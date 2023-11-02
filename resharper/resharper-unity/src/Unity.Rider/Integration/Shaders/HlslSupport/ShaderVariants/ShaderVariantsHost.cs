#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.Rd.Tasks;
using JetBrains.RdBackend.Common.Features.Documents;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Common.Utils;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost : ICppChangeProvider, IEnabledShaderKeywordsProvider, IShaderPlatformInfoProvider
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
    private readonly ChangeManager myChangeManager;
    private readonly IDocumentHost myDocumentHost;
    private readonly IPreferredRootFileProvider myPreferredPreferredRootFileProvider;
    private readonly ILogger myLogger;

    private readonly RdShaderVariant myCurrentVariant;
    private readonly SettingsIndexedEntry myDefaultEnabledKeywordsEntry;
    private readonly SettingsScalarEntry myFeaturePreviewEnabledEntry;
    private readonly SettingsScalarEntry myDefaultShaderApiEntry;
    private readonly RdShaderVariant myDefaultShaderVariant;
    
    private bool mySupportEnabled;

    public ShaderVariantsHost(Lifetime lifetime,
        ISolution solution,
        ShaderProgramCache shaderProgramCache,
        IPreferredRootFileProvider preferredRootFileProvider,
        ISettingsStore settingsStore,
        [UsedImplicitly] SolutionSettingsReadyForSolutionInstanceComponent _,
        ChangeManager changeManager,
        ILogger logger,
        IDocumentHost documentHost,
        FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;
        myChangeManager = changeManager;
        myPreferredPreferredRootFileProvider = preferredRootFileProvider;
        myDocumentHost = documentHost;
        myLogger = logger;
        
        myChangeManager.RegisterChangeProvider(lifetime, this);

        myBoundSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(mySolution.ToDataContext()));
        myDefaultEnabledKeywordsEntry = myBoundSettingsStore.Schema.GetIndexedEntry(static (ShaderVariantsSettings s) => s.EnabledKeywords);
        myDefaultShaderApiEntry = myBoundSettingsStore.Schema.GetScalarEntry(static (ShaderVariantsSettings s) => s.ShaderApi);
        myDefaultShaderVariant = new RdShaderVariant();
        myCurrentVariant = myDefaultShaderVariant;
        myDefaultShaderVariant.EnabledKeywords.UnionWith(EnumEnabledKeywords(myDefaultEnabledKeywordsEntry));
        SyncShaderApi(myDefaultShaderVariant, myDefaultShaderApiEntry);
        
        myFeaturePreviewEnabledEntry = myBoundSettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.FeaturePreviewShaderVariantsSupport);
        mySupportEnabled = myBoundSettingsStore.GetValue(myFeaturePreviewEnabledEntry, null) is true;
        
        myBoundSettingsStore.AdviseAsyncChanged(lifetime, OnBoundSettingsStoreChange);
        
        frontendBackendHost?.Do(model =>
        {
            myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderVariants(model));
            
            myDefaultShaderVariant.EnableKeyword.Advise(lifetime, keyword => SetKeywordEnabled(keyword, true));
            myDefaultShaderVariant.DisableKeyword.Advise(lifetime, keyword => SetKeywordEnabled(keyword, false));
            myDefaultShaderVariant.SetShaderApi.Advise(lifetime, SetShaderApi);

            model.DefaultShaderVariant.Value = myDefaultShaderVariant;
            model.CreateShaderVariantInteraction.SetAsync(HandleCreateShaderVariantInteraction);
        });
    }

    private Task OnBoundSettingsStoreChange(Lifetime lifetime, SettingsStoreChangeArgs args)
    {
        Action<ChangeTracker>? work = null;
        if (args.ChangedEntries.Contains(myDefaultEnabledKeywordsEntry))
            work += changeTracker => SyncEnabledKeywords(changeTracker, myDefaultShaderVariant, EnumEnabledKeywords(myDefaultEnabledKeywordsEntry));
        if (args.ChangedEntries.Contains(myDefaultShaderApiEntry))
            work += changeTracker =>
            {
                SyncShaderApi(myDefaultShaderVariant, myDefaultShaderApiEntry);
                changeTracker.MarkAllDirty();
            };
        if (args.ChangedEntries.Contains(myFeaturePreviewEnabledEntry))
            work += changeTracker =>
            {
                mySupportEnabled = myBoundSettingsStore.GetValue(myFeaturePreviewEnabledEntry, null) is true;
                changeTracker.MarkAllDirty();
            };
        return work != null ? lifetime.StartMainRead(() =>
        {
            using var changeTracker = new ChangeTracker(this);
            work.Invoke(changeTracker);
        }) : Task.CompletedTask;
    }

    private async Task<ShaderVariantInteraction> HandleCreateShaderVariantInteraction(Lifetime lifetime, CreateShaderVariantInteractionArgs args)
    {
        myLogger.Verbose("Start shader variant interaction");
        var keywords = await lifetime.StartBackgroundRead(() =>
        {
            var document = myDocumentHost.TryGetDocument(args.DocumentId);
            if (document == null)
                return new List<string>();
                    
            CppFileLocation rootLocation;
            if (document.Location.Name.EndsWith(ShaderLabProjectFileType.SHADERLAB_EXTENSION))
            {
                if (document.GetPsiSourceFile(mySolution) is { } sourceFile &&
                    sourceFile.GetPrimaryPsiFile()?.FindTokenAt(new TreeOffset(args.Offset)) is { } token &&
                    token.NodeType == ShaderLabTokenType.CG_CONTENT)
                    rootLocation = new CppFileLocation(sourceFile, TextRange.FromLength(token.GetTreeStartOffset().Offset, token.GetTextLength()));
            }
            else
                rootLocation = myPreferredPreferredRootFileProvider.GetPreferredRootFile(new CppFileLocation(document.Location));

            if (!rootLocation.IsValid() || !myShaderProgramCache.TryGetShaderProgramInfo(rootLocation, out var shaderProgramInfo))
                return new List<string>();
            return shaderProgramInfo.Keywords.ToList();
        });
        return new ShaderVariantInteraction(keywords);
    }

    private IEnumerable<string> EnumEnabledKeywords(SettingsIndexedEntry entry) => myBoundSettingsStore.EnumIndexedValues(entry, null).Keys.Cast<string>();
    
    private void SyncEnabledKeywords(ChangeTracker changeTracker, RdShaderVariant shaderVariant, IEnumerable<string> newKeywords)
    {
        var unprocessed = shaderVariant.EnabledKeywords.ToHashSet();
        foreach (var keyword in newKeywords)
        {
            if (!unprocessed.Remove(keyword))
            {
                shaderVariant.EnabledKeywords.Add(keyword);
                changeTracker.MarkShaderKeywordDirty(keyword);
            }
        }

        foreach (var removed in unprocessed)
        {
            shaderVariant.EnabledKeywords.Remove(removed);
            changeTracker.MarkShaderKeywordDirty(removed);
        }
    }

    private void SetKeywordEnabled(string keyword, bool enabled)
    {
        switch (enabled)
        {
            case true:
                myBoundSettingsStore.SetIndexedValue(static (ShaderVariantsSettings s) => s.EnabledKeywords, keyword, true);
                break;
            case false:
                myBoundSettingsStore.RemoveIndexedValue(static (ShaderVariantsSettings s) => s.EnabledKeywords, keyword);
                break;
        }
    }

    private void SetShaderApi(RdShaderApi rdShaderApi) => myBoundSettingsStore.SetValue(static (ShaderVariantsSettings s) => s.ShaderApi, rdShaderApi.AsShaderApi());

    private void SyncShaderApi(RdShaderVariant rdShaderVariant, SettingsScalarEntry shaderApiEntry) => 
        rdShaderVariant.ShaderApi.Value = myBoundSettingsStore.GetValue(shaderApiEntry, null) is ShaderApi shaderApi ? shaderApi.AsRdShaderApi() : RdShaderApi.D3D11;

    private void SyncShaderVariants(FrontendBackendModel model)
    {
        // State modifications only allowed from main thread  
        mySolution.Locks.AssertMainThread();
        
        using var readLockCookie = ReadLockCookie.Create();
        var unprocessedKeywords = model.ShaderKeywords.Keys.ToSet();
        myShaderProgramCache.ForEachKeyword(keyword =>
        {
            if (!unprocessedKeywords.Remove(keyword))
                model.ShaderKeywords[keyword] = new(keyword);
        });

        using var changeTracker = new ChangeTracker(this); 
        foreach (var removedKeyword in unprocessedKeywords)
        {
            model.ShaderKeywords.Remove(removedKeyword);
            if (model.DefaultShaderVariant.Value.EnabledKeywords.Remove(removedKeyword))
                changeTracker.MarkShaderKeywordDirty(removedKeyword);
        }
    }

    public ISet<string> GetEnabledKeywords(CppFileLocation location) => mySupportEnabled ? myCurrentVariant.EnabledKeywords : EmptySet<string>.Instance;
    
    public ShaderApi ShaderApi => mySupportEnabled ? myCurrentVariant.ShaderApi.Value.AsShaderApi() : ShaderApi.D3D11;

    public object? Execute(IChangeMap changeMap) => null;
    
    private struct ChangeTracker : IDisposable, IValueAction<CppFileLocation>
    {
        private readonly ShaderVariantsHost myHost;
        private readonly HashSet<CppFileLocation> myOutdatedLocations = new();

        public ChangeTracker(ShaderVariantsHost host)
        {
            myHost = host;
        }

        void IValueAction<CppFileLocation>.Invoke(CppFileLocation location) => myOutdatedLocations.Add(location);

        public void MarkShaderKeywordDirty(string shaderKeyword) => myHost.myShaderProgramCache.ForEachKeywordLocation(shaderKeyword, ref this);

        public void MarkAllDirty() => myHost.myShaderProgramCache.CollectLocationsTo(myOutdatedLocations);

        public void Dispose()
        {
            if (!myOutdatedLocations.IsEmpty())
            {
                using (WriteLockCookie.Create())
                {
                    myHost.myChangeManager.OnProviderChanged(myHost, new CppChange(myOutdatedLocations), SimpleTaskExecutor.Instance);
                    FixMeInvalidateInjectedFiles();
                }
            }
        }

        private void FixMeInvalidateInjectedFiles()
        {
            foreach (var location in myOutdatedLocations)
            {
                if (location.IsInjected())
                    InjectedHlslUtils.InvalidatePsiForInjectedLocation(myHost.mySolution, location);
            }
        }
    }
}