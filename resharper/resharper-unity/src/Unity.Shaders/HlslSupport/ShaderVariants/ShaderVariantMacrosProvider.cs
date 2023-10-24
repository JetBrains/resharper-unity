#nullable enable
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent]
public class ShaderVariantMacrosProvider : IUnityHlslCustomMacrosProvider
{
    private readonly IEnabledShaderKeywordsProvider? myEnabledShaderKeywordsProvider;

    public ShaderVariantMacrosProvider([Optional] IEnabledShaderKeywordsProvider? enabledShaderKeywordsProvider)
    {
        myEnabledShaderKeywordsProvider = enabledShaderKeywordsProvider;
    }

    public IEnumerable<CppPPDefineSymbol> ProvideCustomMacros(CppFileLocation location, ShaderProgramInfo shaderProgramInfo)
    {
        var enabledKeywords = myEnabledShaderKeywordsProvider?.GetEnabledKeywords(location) ?? EmptySet<string>.InstanceSet;
        foreach (var shaderFeature in shaderProgramInfo.ShaderFeatures)
        {
            if (TryGetEnabledKeyword(shaderFeature, enabledKeywords, out var entry))
            {
                // TODO: can't use real location, because symbol not registered in symbol table. Have to support symbols from shader features in C++ engine.  
                var symbolLocation = new CppSymbolLocation(CppFileLocation.EMPTY, new CppComplexOffset(entry.TextRange.StartOffset));
                yield return new CppPPDefineSymbol(entry.Keyword, null, false, "1", symbolLocation);
            }
        }
    }
    
    private static bool TryGetEnabledKeyword(ShaderFeature shaderFeature, ISet<string> enabledKeywords, out ShaderFeature.Entry entry)
    {
        var entries = shaderFeature.Entries;
        if (!enabledKeywords.IsEmpty())
        {
            foreach (var candidate in entries)
            {
                if (enabledKeywords.Contains(candidate.Keyword))
                {
                    entry = candidate;
                    return true;
                }
            }
        }

        if (shaderFeature is { AllowAllDisabled: false, Entries.Length: > 0 })
        {
            entry = shaderFeature.Entries[0];
            return true;
        }

        entry = default;
        return false;
    }
}
