#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Common.Utils;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsManager : ICppChangeProvider
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly ChangeManager myChangeManager;

    private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
    private readonly SettingsIndexedEntry myDefaultEnabledKeywordsEntry;
    private readonly SettingsScalarEntry myFeaturePreviewEnabledEntry;
    private readonly SettingsScalarEntry myDefaultShaderApiEntry;
    
    private readonly ViewableSet<string> myEnabledKeywords = new();
    private readonly HashSet<string> myAllKeywords = new();

    private readonly ViewableProperty<int> myTotalKeywordsCount = new(0);
    private readonly ViewableProperty<int> myTotalEnabledKeywordsCount = new(0);
    
    private ShaderApi myShaderApi;
    private bool mySupportEnabled;

    public ShaderVariantsManager(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, ShaderProgramCache shaderProgramCache, ChangeManager changeManager)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;
        myChangeManager = changeManager;

        myChangeManager.RegisterChangeProvider(lifetime, this);
        
        myBoundSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
        myDefaultEnabledKeywordsEntry = myBoundSettingsStore.Schema.GetIndexedEntry(static (ShaderVariantsSettings s) => s.EnabledKeywords);
        myDefaultShaderApiEntry = myBoundSettingsStore.Schema.GetScalarEntry(static (ShaderVariantsSettings s) => s.ShaderApi);
        myEnabledKeywords.UnionWith(EnumEnabledKeywords(myDefaultEnabledKeywordsEntry));
        myShaderApi = GetShaderApi(myDefaultShaderApiEntry);
        
        myFeaturePreviewEnabledEntry = myBoundSettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.FeaturePreviewShaderVariantsSupport);
        mySupportEnabled = myBoundSettingsStore.GetValue(myFeaturePreviewEnabledEntry, null) is true;
        
        myBoundSettingsStore.AdviseAsyncChanged(lifetime, OnBoundSettingsStoreChange);
        myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderKeywords(myEnabledKeywords));
    }
    
    public ShaderApi ShaderApi => mySupportEnabled ? myShaderApi : ShaderApi.D3D11;

    public IReadonlyProperty<int> TotalKeywordsCount => myTotalKeywordsCount;

    public IReadonlyProperty<int> TotalEnabledKeywordsCount => myTotalEnabledKeywordsCount;

    public IViewableSet<string> AllEnabledKeywords => myEnabledKeywords;

    public void SetDefineSymbolEnabled(string symbol, bool enabled)
    {
        if (ShaderDefineSymbolsRecognizer.Recognize(symbol) is { } descriptor)
        {
            if (descriptor is ShaderApiDefineSymbolDescriptor shaderApiDescriptor) 
                SetShaderApi(enabled ? shaderApiDescriptor.GetValue(symbol) : ShaderApiDefineSymbolDescriptor.DefaultValue);
        }
        else if (myShaderProgramCache.HasShaderKeyword(symbol))
            SetKeywordEnabled(symbol, enabled);
    }
    
    public void SetKeywordEnabled(string keyword, bool enabled)
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
    
    public void SetShaderApi(ShaderApi shaderApi) => myBoundSettingsStore.SetValue(static (ShaderVariantsSettings s) => s.ShaderApi, shaderApi);

    public void ResetAllKeywords()
    {
        var entry = myDefaultEnabledKeywordsEntry;
        foreach (var index in myBoundSettingsStore.EnumIndexedValues(entry, null).Keys.ToList()) 
            myBoundSettingsStore.RemoveIndexedValue(entry, index);
    }

    public ISet<string> GetEnabledKeywords(CppFileLocation location) => mySupportEnabled ? myEnabledKeywords : EmptySet<string>.Instance;

    public bool IsKeywordEnabled(string keyword) => myEnabledKeywords.Contains(keyword);
    
    object? IChangeProvider.Execute(IChangeMap changeMap) => null;

    private Task OnBoundSettingsStoreChange(Lifetime lifetime, SettingsStoreChangeArgs args)
    {
        Action<ChangeTracker>? work = null;
        if (args.ChangedEntries.Contains(myDefaultEnabledKeywordsEntry))
            work += changeTracker =>
            {
                SyncEnabledKeywords(changeTracker, myEnabledKeywords, EnumEnabledKeywords(myDefaultEnabledKeywordsEntry));
                myTotalEnabledKeywordsCount.Value = myEnabledKeywords.Count;
            };
        if (args.ChangedEntries.Contains(myDefaultShaderApiEntry))
            work += changeTracker =>
            {
                myShaderApi = GetShaderApi(myDefaultShaderApiEntry);
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
    
    private ShaderApi GetShaderApi(SettingsScalarEntry shaderApiEntry) => myBoundSettingsStore.GetValue(shaderApiEntry, null) is ShaderApi shaderApi ? shaderApi : ShaderApiDefineSymbolDescriptor.DefaultValue;
    
    private void SyncShaderKeywords(ISet<string> enabledKeywords)
    {
        // State modifications only allowed from main thread  
        mySolution.Locks.AssertMainThread();
        
        using var readLockCookie = ReadLockCookie.Create();
        var unprocessedKeywords = myAllKeywords.ToSet();
        myShaderProgramCache.ForEachKeyword(keyword =>
        {
            if (!unprocessedKeywords.Remove(keyword))
                myAllKeywords.Add(keyword);
        });

        using var changeTracker = new ChangeTracker(this); 
        foreach (var removedKeyword in unprocessedKeywords)
        {
            myAllKeywords.Remove(removedKeyword);
            if (enabledKeywords.Remove(removedKeyword))
                changeTracker.MarkShaderKeywordDirty(removedKeyword);
        }

        myTotalKeywordsCount.Value = myAllKeywords.Count;
    }
    
    private IEnumerable<string> EnumEnabledKeywords(SettingsIndexedEntry entry) => myBoundSettingsStore.EnumIndexedValues(entry, null).Keys.Cast<string>();
    
    private void SyncEnabledKeywords(ChangeTracker changeTracker, ISet<string> enabledKeywords, IEnumerable<string> newKeywords)
    {
        var unprocessed = enabledKeywords.ToHashSet();
        foreach (var keyword in newKeywords)
        {
            if (!unprocessed.Remove(keyword))
            {
                enabledKeywords.Add(keyword);
                changeTracker.MarkShaderKeywordDirty(keyword);
            }
        }

        foreach (var removed in unprocessed)
        {
            enabledKeywords.Remove(removed);
            changeTracker.MarkShaderKeywordDirty(removed);
        }
    }
    
    private struct ChangeTracker : IDisposable, IValueAction<CppFileLocation>
    {
        private readonly ShaderVariantsManager myManager;
        private readonly HashSet<CppFileLocation> myOutdatedLocations = new();

        public ChangeTracker(ShaderVariantsManager manager)
        {
            myManager = manager;
        }

        void IValueAction<CppFileLocation>.Invoke(CppFileLocation location) => myOutdatedLocations.Add(location);

        public void MarkShaderKeywordDirty(string shaderKeyword) => myManager.myShaderProgramCache.ForEachKeywordLocation(shaderKeyword, ref this);

        public void MarkAllDirty() => myManager.myShaderProgramCache.CollectLocationsTo(myOutdatedLocations);

        public void Dispose()
        {
            if (!myOutdatedLocations.IsEmpty())
            {
                using (WriteLockCookie.Create())
                {
                    myManager.myChangeManager.OnProviderChanged(myManager, new CppChange(myOutdatedLocations), SimpleTaskExecutor.Instance);
                    FixMeInvalidateInjectedFiles();
                }
            }
        }

        private void FixMeInvalidateInjectedFiles()
        {
            foreach (var location in myOutdatedLocations)
            {
                if (location.IsInjected())
                    InjectedHlslUtils.InvalidatePsiForInjectedLocation(myManager.mySolution, location);
            }
        }
    }
}