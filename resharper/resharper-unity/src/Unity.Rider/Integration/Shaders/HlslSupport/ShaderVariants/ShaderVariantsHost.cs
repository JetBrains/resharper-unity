#nullable enable
using System;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost
{
    private readonly ISolution mySolution;
    private readonly ISettingsStore mySettingsStore;
    private readonly ShaderProgramCache myShaderProgramCache;

    public ShaderVariantsHost(Lifetime lifetime, ISolution solution, ShaderProgramCache shaderProgramCache, ISettingsStore settingsStore, FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;
        mySettingsStore = settingsStore;

        frontendBackendHost?.Do(model =>
        {
            var defaultSet = new RdShaderVariantSet();
            
            var boundedStore = mySettingsStore.BindToContextTransient(ContextRange.Smart(mySolution.ToDataContext()));
            var selectedVariants = boundedStore.EnumIndexedValues((ShaderVariantsSettings s) => s.SelectedVariants);
            defaultSet.SelectedVariants.UnionWith(selectedVariants);
            
            model.DefaultShaderVariantSet.Value = defaultSet;
            myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderVariants(model));
            
            model.DefaultShaderVariantSet.Value.SelectedVariants.Change.Advise(lifetime, OnSelectedVariantsChanged);
        });
    }

    private void OnSelectedVariantsChanged(SetEvent<string> evt)
    {
        var boundedStore = mySettingsStore.BindToContextTransient(ContextRange.Smart(mySolution.ToDataContext())); 
        switch (evt.Kind)
        {
            case AddRemove.Add:
                boundedStore.SetIndexedValue(static (ShaderVariantsSettings s) => s.SelectedVariants, evt.Value, evt.Value);
                break;
            case AddRemove.Remove:
                boundedStore.RemoveIndexedValue(static (ShaderVariantsSettings s) => s.SelectedVariants, evt.Value);
                break;
            default:
                throw new ArgumentOutOfRangeException();
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
}