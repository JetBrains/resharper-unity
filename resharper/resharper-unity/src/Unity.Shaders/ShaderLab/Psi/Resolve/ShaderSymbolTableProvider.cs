#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve;

[SolutionComponent(InstantiationEx.LegacyDefault)]
public class ShaderSymbolTableProvider(ShaderLabCache shaderLabCache, IPsiServices psiServices)
{
    private readonly BuiltinShadersSymbolTable myBuiltinShadersSymbolTable = new(psiServices);

    /// <summary>Returns table of all shaders declared with ShaderLab + built-in shaders.</summary>
    public ISymbolTable GetSymbolTable()
    {
        var values = shaderLabCache.LocalCacheValues;
        if (values.Count == 0)
            return myBuiltinShadersSymbolTable;

        return new DeclaredElementsSymbolTable<IDeclaredElement>(psiServices, values.ToIList())
            .Merge(myBuiltinShadersSymbolTable);
    }
}