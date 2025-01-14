#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Common.Utils;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class ShaderVariantsManager : ICppChangeProvider, ISolutionChangeProvider
{
    private readonly Lifetime myLifetime;
    private readonly ISolution mySolution;
    private readonly ILazy<ShaderProgramCache> myLazyShaderProgramCache;
    private ShaderProgramCache MyShaderProgramCache => myLazyShaderProgramCache.Value;
    private readonly ChangeManager myChangeManager;

    private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
    private readonly SettingsIndexedEntry myDefaultEnabledKeywordsEntry;
    private readonly SettingsScalarEntry myDefaultShaderApiEntry;
    private readonly SettingsScalarEntry myShaderPlatformEntry;
    private readonly SettingsScalarEntry myUrtCompilationModeEntry;
    
    private readonly ViewableSet<string> myEnabledKeywords = new();
    private readonly HashSet<string> myAllKeywords = new();

    private readonly ViewableProperty<int> myTotalKeywordsCount = new(0);
    private readonly ViewableProperty<int> myTotalEnabledKeywordsCount = new(0);

    public ShaderVariantsManager(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, ILazy<ShaderProgramCache> shaderProgramCache, ChangeManager changeManager, UnitySolutionTracker unitySolutionTracker)
    {
        myLifetime = lifetime;
        mySolution = solution;
        myLazyShaderProgramCache = shaderProgramCache;
        myChangeManager = changeManager;

        myBoundSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()));
        myDefaultEnabledKeywordsEntry = myBoundSettingsStore.Schema.GetIndexedEntry(static (ShaderVariantsSettings s) => s.EnabledKeywords);
        myDefaultShaderApiEntry = myBoundSettingsStore.Schema.GetScalarEntry(static (ShaderVariantsSettings s) => s.ShaderApi);
        myShaderPlatformEntry = myBoundSettingsStore.Schema.GetScalarEntry(static (ShaderVariantsSettings s) => s.ShaderPlatform);
        myUrtCompilationModeEntry = myBoundSettingsStore.Schema.GetScalarEntry(static (ShaderVariantsSettings s) => s.UrtCompilationMode);
        myEnabledKeywords.UnionWith(EnumEnabledKeywords(myDefaultEnabledKeywordsEntry));
        ShaderApi = GetShaderApi(myDefaultShaderApiEntry);
        ShaderPlatform = GetShaderPlatform(myShaderPlatformEntry); 
        UrtCompilationMode = GetUrtCompilationMode(myUrtCompilationModeEntry); 
        
        myBoundSettingsStore.AdviseAsyncChanged(lifetime, OnBoundSettingsStoreChange);
        unitySolutionTracker.IsUnityProjectFolder.AdviseUntil(lifetime, res =>
        {
            if (res)
            {
                MyShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderKeywords(myEnabledKeywords));
                return true;
            }

            return false;
        });
        
    }

    Lifetime ISolutionChangeProvider.Lifetime => myLifetime;

    public ShaderApi ShaderApi { get; private set; }

    public ShaderPlatform ShaderPlatform { get; private set; }
    public UrtCompilationMode UrtCompilationMode { get; private set; }

    public IReadonlyProperty<int> TotalKeywordsCount => myTotalKeywordsCount;

    public IReadonlyProperty<int> TotalEnabledKeywordsCount => myTotalEnabledKeywordsCount;

    public IViewableSet<string> AllEnabledKeywords => myEnabledKeywords;

    public void SetDefineSymbolEnabled(string symbol, bool enabled)
    {
        if (ShaderDefineSymbolsRecognizer.Recognize(symbol) is { } descriptor)
        {
            if (descriptor is ShaderApiDefineSymbolDescriptor shaderApiDescriptor) 
                SetShaderApi(enabled ? shaderApiDescriptor.GetValue(symbol) : ShaderApiDefineSymbolDescriptor.DefaultValue);
            else if (descriptor is ShaderPlatformDefineSymbolDescriptor shaderPlatformDescriptor)
                SetShaderPlatform(enabled ? shaderPlatformDescriptor.GetValue(symbol) : ShaderPlatformDefineSymbolDescriptor.DefaultValue);
            else if (descriptor is UrtCompilationModeDefineSymbolDescriptor urtModeDescriptor)
                SetUrtCompilationMode(enabled ? urtModeDescriptor.GetValue(symbol) : UrtCompilationModeDefineSymbolDescriptor.DefaultValue);
        }
        else if (MyShaderProgramCache.HasShaderKeyword(symbol))
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
    
    public void SetKeywordsEnabled(IEnumerable<string> keywords, bool enabled)
    {
        foreach (var keyword in keywords)
            SetKeywordEnabled(keyword, enabled);
    }
    
    public void SetShaderApi(ShaderApi shaderApi) => myBoundSettingsStore.SetValue(static (ShaderVariantsSettings s) => s.ShaderApi, shaderApi);
    
    public void SetShaderPlatform(ShaderPlatform shaderPlatform) => myBoundSettingsStore.SetValue(static (ShaderVariantsSettings s) => s.ShaderPlatform, shaderPlatform);
    public void SetUrtCompilationMode(UrtCompilationMode urtCompilationMode) => myBoundSettingsStore.SetValue(static (ShaderVariantsSettings s) => s.UrtCompilationMode, urtCompilationMode);

    public void ResetAllKeywords()
    {
        var entry = myDefaultEnabledKeywordsEntry;
        foreach (var index in myBoundSettingsStore.EnumIndexedValues(entry, null).Keys.ToList()) 
            myBoundSettingsStore.RemoveIndexedValue(entry, index);
    }

    public ISet<string> GetEnabledKeywords(CppFileLocation _) => myEnabledKeywords;

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
                ShaderApi = GetShaderApi(myDefaultShaderApiEntry);
                changeTracker.MarkAllDirty();
            };
        if (args.ChangedEntries.Contains(myShaderPlatformEntry))
            work += changeTracker =>
            {
                ShaderPlatform = GetShaderPlatform(myShaderPlatformEntry);
                changeTracker.MarkAllDirty();
            };
        if (args.ChangedEntries.Contains(myUrtCompilationModeEntry))
            work += changeTracker =>
            {
                UrtCompilationMode = GetUrtCompilationMode(myUrtCompilationModeEntry);
                changeTracker.MarkAllDirty();
            };
        return work != null ? lifetime.StartMainRead(() =>
        {
            using var changeTracker = new ChangeTracker(this);
            work.Invoke(changeTracker);
        }) : Task.CompletedTask;
    }
    
    private ShaderApi GetShaderApi(SettingsScalarEntry shaderApiEntry) => myBoundSettingsStore.GetValue(shaderApiEntry, null) is ShaderApi shaderApi ? shaderApi : ShaderApiDefineSymbolDescriptor.DefaultValue;
    
    private ShaderPlatform GetShaderPlatform(SettingsScalarEntry shaderPlatformEntry) => myBoundSettingsStore.GetValue(shaderPlatformEntry, null) is ShaderPlatform shaderApi ? shaderApi : ShaderPlatformDefineSymbolDescriptor.DefaultValue;
    private UrtCompilationMode GetUrtCompilationMode(SettingsScalarEntry urtCompilationMode) => myBoundSettingsStore.GetValue(urtCompilationMode, null) is UrtCompilationMode urtMode ? urtMode : UrtCompilationModeDefineSymbolDescriptor.DefaultValue;
    
    private void SyncShaderKeywords(ISet<string> enabledKeywords)
    {
        // State modifications only allowed from main thread  
        mySolution.Locks.AssertMainThread();
        
        using var readLockCookie = ReadLockCookie.Create();
        var unprocessedKeywords = myAllKeywords.ToSet();
        MyShaderProgramCache.ForEachKeyword(keyword =>
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
    
    private struct ChangeTracker(ShaderVariantsManager manager) : IDisposable, IValueAction<CppFileLocation>
    {
        private readonly HashSet<CppFileLocation> myOutdatedLocations = new();

        void IValueAction<CppFileLocation>.Invoke(CppFileLocation location) => myOutdatedLocations.Add(location);

        public void MarkShaderKeywordDirty(string shaderKeyword) => manager.MyShaderProgramCache.ForEachKeywordLocation(shaderKeyword, ref this);

        public void MarkAllDirty() => manager.MyShaderProgramCache.CollectLocationsTo(myOutdatedLocations);

        public void Dispose()
        {
            if (!myOutdatedLocations.IsEmpty())
            {
                using (WriteLockCookie.Create())
                {
                    manager.myChangeManager.OnProviderChanged(manager, new CppChange(myOutdatedLocations), SimpleTaskExecutor.Instance);
                    FixMeInvalidateInjectedFiles();
                }
            }
        }

        private void FixMeInvalidateInjectedFiles()
        {
            foreach (var location in myOutdatedLocations)
            {
                if (location.IsInjected())
                    InjectedHlslUtils.InvalidatePsiForInjectedLocation(manager.mySolution, location);
            }
        }
    }
}