#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost : IUnityHlslCustomDefinesProvider
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

    private readonly RdShaderVariantSet myCurrentVariantSet; 

    public ShaderVariantsHost(Lifetime lifetime, ISolution solution, ShaderProgramCache shaderProgramCache, ISettingsStore settingsStore, [UsedImplicitly] SolutionSettingsReadyForSolutionInstanceComponent _, FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;

        myBoundSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(mySolution.ToDataContext()));
        var defaultSelectedVariantsEntry = myBoundSettingsStore.Schema.GetIndexedEntry(static (ShaderVariantsSettings s) => s.SelectedVariants);
        var defaultSet = new RdShaderVariantSet();
        myCurrentVariantSet = defaultSet;
        defaultSet.SelectedVariants.UnionWith(EnumSelectedVariants(defaultSelectedVariantsEntry));
        myBoundSettingsStore.AdviseAsyncChanged(lifetime, (lt, args) =>
        {
            if (!args.ChangedEntries.Contains(defaultSelectedVariantsEntry))
                return Task.CompletedTask;
            return lt.StartMainRead(() => SyncSelectedVariants(defaultSet, EnumSelectedVariants(defaultSelectedVariantsEntry)));
        });
        
        frontendBackendHost?.Do(model =>
        {
            model.DefaultShaderVariantSet.Value = defaultSet;
            myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderVariants(model));
            
            model.DefaultShaderVariantSet.Value.SelectVariant.Advise(lifetime, variant => SetVariantSelected(variant, true));
            model.DefaultShaderVariantSet.Value.DeselectVariant.Advise(lifetime, variant => SetVariantSelected(variant, false));
        });
    }

    private IEnumerable<string> EnumSelectedVariants(SettingsIndexedEntry entry) => myBoundSettingsStore.EnumIndexedValues(entry, null).Values.Cast<string>();
    
    private void SyncSelectedVariants(RdShaderVariantSet shaderVariantSet, IEnumerable<string> newVariants)
    {
        var unprocessed = shaderVariantSet.SelectedVariants.ToHashSet();
        foreach (var shaderVariant in newVariants)
        {
            if (!unprocessed.Remove(shaderVariant))
                shaderVariantSet.SelectedVariants.Add(shaderVariant);
        }

        foreach (var removed in unprocessed)
        {
            shaderVariantSet.SelectedVariants.Remove(removed);
        }
    }

    private void SetVariantSelected(string variant, bool selected)
    {
        switch (selected)
        {
            case true:
                myBoundSettingsStore.SetIndexedValue(static (ShaderVariantsSettings s) => s.SelectedVariants, variant, variant);
                break;
            case false:
                myBoundSettingsStore.RemoveIndexedValue(static (ShaderVariantsSettings s) => s.SelectedVariants, variant);
                break;
        }
    }

    private void SyncShaderVariants(FrontendBackendModel model)
    {
        // State modifications only allowed from main thread  
        mySolution.Locks.AssertMainThread();
        
        using var readLockCookie = ReadLockCookie.Create();
        var oldKeys = model.ShaderVariants.Keys.ToSet();
        myShaderProgramCache.ForEachVariant(variant =>
        {
            if (!oldKeys.Remove(variant))
                model.ShaderVariants[variant] = new(variant);
        });
        foreach (var removedKey in oldKeys)
        {
            model.ShaderVariants.Remove(removedKey);
            model.DefaultShaderVariantSet.Value.SelectedVariants.Remove(removedKey);
        }
    }

    public IEnumerable<string> ProvideCustomDefines(UnityHlslDialectBase dialect)
    {
        return dialect switch
        {
            UnityComputeHlslDialect => EmptyList<string>.Enumerable,
            _ => myCurrentVariantSet.SelectedVariants
        };
    }
}