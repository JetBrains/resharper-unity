#nullable enable
using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;
    private readonly IContextBoundSettingsStore myBoundSettingsStore;

    public ShaderVariantsHost(Lifetime lifetime, ISolution solution, ShaderProgramCache shaderProgramCache, ISettingsStore settingsStore, [UsedImplicitly] SolutionSettingsReadyForSolutionInstanceComponent _, FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;

        myBoundSettingsStore = settingsStore.BindToContextTransient(ContextRange.Smart(mySolution.ToDataContext()));
        var selectedVariants = myBoundSettingsStore.EnumIndexedValues(static (ShaderVariantsSettings s) => s.SelectedVariants);
        
        var defaultSet = new RdShaderVariantSet();
        defaultSet.SelectedVariants.UnionWith(selectedVariants);
        
        frontendBackendHost?.Do(model =>
        {
            model.DefaultShaderVariantSet.Value = defaultSet;
            myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderVariants(model));
            
            model.DefaultShaderVariantSet.Value.SelectedVariants.Change.Advise(lifetime, OnSelectedVariantsChanged);
        });
    }

    private void OnSelectedVariantsChanged(SetEvent<string> evt)
    {
        switch (evt.Kind)
        {
            case AddRemove.Add:
                myBoundSettingsStore.SetIndexedValue(static (ShaderVariantsSettings s) => s.SelectedVariants, evt.Value, evt.Value);
                break;
            case AddRemove.Remove:
                myBoundSettingsStore.RemoveIndexedValue(static (ShaderVariantsSettings s) => s.SelectedVariants, evt.Value);
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