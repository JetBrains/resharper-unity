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
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Common.Utils;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost : ICppChangeProvider, IEnabledShaderKeywordsProvider
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
    private readonly ChangeManager myChangeManager;

    private readonly RdShaderVariant myCurrentVariant; 

    public ShaderVariantsHost(Lifetime lifetime, ISolution solution, ShaderProgramCache shaderProgramCache, ISettingsStore settingsStore, [UsedImplicitly] SolutionSettingsReadyForSolutionInstanceComponent _, ChangeManager changeManager, FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;
        myChangeManager = changeManager;
        myChangeManager.RegisterChangeProvider(lifetime, this);

        myBoundSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(mySolution.ToDataContext()));
        var defaultEnabledKeywordsEntry = myBoundSettingsStore.Schema.GetIndexedEntry(static (ShaderVariantsSettings s) => s.EnabledKeywords);
        var defaultShaderVariant = new RdShaderVariant();
        myCurrentVariant = defaultShaderVariant;
        defaultShaderVariant.EnabledKeywords.UnionWith(EnumEnabledKeywords(defaultEnabledKeywordsEntry));
        myBoundSettingsStore.AdviseAsyncChanged(lifetime, (lt, args) =>
        {
            if (!args.ChangedEntries.Contains(defaultEnabledKeywordsEntry))
                return Task.CompletedTask;
            return lt.StartMainRead(() => SyncEnabledKeywords(defaultShaderVariant, EnumEnabledKeywords(defaultEnabledKeywordsEntry)));
        });
        
        frontendBackendHost?.Do(model =>
        {
            myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderVariants(model));
            
            defaultShaderVariant.EnableKeyword.Advise(lifetime, keyword => SetKeywordEnabled(keyword, true));
            defaultShaderVariant.DisableKeyword.Advise(lifetime, keyword => SetKeywordEnabled(keyword, false));

            model.DefaultShaderVariant.Value = defaultShaderVariant;
        });
    }

    private IEnumerable<string> EnumEnabledKeywords(SettingsIndexedEntry entry) => myBoundSettingsStore.EnumIndexedValues(entry, null).Keys.Cast<string>();
    
    private void SyncEnabledKeywords(RdShaderVariant shaderVariant, IEnumerable<string> newKeywords)
    {
        using var changeTracker = new ChangeTracker(this);
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

    public ISet<string> GetEnabledKeywords(CppFileLocation location) => myCurrentVariant.EnabledKeywords;

    public object? Execute(IChangeMap changeMap) => null;
    
    private struct ChangeTracker : IDisposable, IValueAction<CppFileLocation>
    {
        private readonly ShaderVariantsHost myHost;
        private FrugalLocalHashSet<CppFileLocation> myOutdatedLocations = new();

        public ChangeTracker(ShaderVariantsHost host)
        {
            myHost = host;
        }

        void IValueAction<CppFileLocation>.Invoke(CppFileLocation location) => myOutdatedLocations.Add(location);

        public void MarkShaderKeywordDirty(string shaderKeyword) => myHost.myShaderProgramCache.ForEachLocation(shaderKeyword, ref this);

        public void Dispose()
        {
            if (!myOutdatedLocations.IsEmpty)
            {
                using (WriteLockCookie.Create())
                    myHost.myChangeManager.OnProviderChanged(myHost, new CppChange(myOutdatedLocations.ReadOnlyCollection()), SimpleTaskExecutor.Instance);
            }
        }
    }
}