#nullable enable
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantsHost
{
    private readonly ISolution mySolution;
    private readonly ShaderProgramCache myShaderProgramCache;

    public ShaderVariantsHost(Lifetime lifetime, ISolution solution, ShaderProgramCache shaderProgramCache, FrontendBackendHost? frontendBackendHost = null)
    {
        mySolution = solution;
        myShaderProgramCache = shaderProgramCache;

        frontendBackendHost?.Do(model =>
        {
            model.DefaultShaderVariantSet.Value = new RdShaderVariantSet();
            myShaderProgramCache.CacheUpdated.Advise(lifetime, _ => SyncShaderVariants(model));
        });
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
            model.ShaderVariants.Remove(removedKey);
    }
}