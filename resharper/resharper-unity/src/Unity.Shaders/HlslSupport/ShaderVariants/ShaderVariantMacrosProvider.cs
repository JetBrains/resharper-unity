#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

[SolutionComponent(Instantiation.DemandAnyThread)]
public class ShaderVariantMacrosProvider : IUnityHlslCustomMacrosProvider
{
    private readonly ShaderVariantsManager myShaderVariantsManager;
    // TODO: Unity team will add _DECLARED_KEYWORD suffix for every shader keyword symbol in feature releases, need to decide based on Unity version if we need to add extra define symbols 
    private readonly bool myAddDeclaredKeywordsSymbols = false;

    public ShaderVariantMacrosProvider(ShaderVariantsManager shaderVariantsManager)
    {
        myShaderVariantsManager = shaderVariantsManager;
    }

    public IEnumerable<CppPPDefineSymbol> ProvideCustomMacros(CppFileLocation location, ShaderProgramInfo? shaderProgramInfo)
    {
        if (shaderProgramInfo != null)
        {
            var enabledKeywords = myShaderVariantsManager.GetEnabledKeywords(location);
            foreach (var shaderFeature in shaderProgramInfo.ShaderFeatures)
            {
                if (TryGetEnabledKeyword(shaderFeature, enabledKeywords, out var entry))
                {
                    // TODO: can't use real location, because symbol not registered in symbol table. Have to support symbols from shader features in C++ engine.  
                    var symbolLocation = new CppSymbolLocation(CppFileLocation.EMPTY, new CppComplexOffset(entry.TextRange.StartOffset));
                    yield return new CppPPDefineSymbol(entry.Keyword, null, false, "1", symbolLocation);
                    if (myAddDeclaredKeywordsSymbols)
                        yield return new CppPPDefineSymbol(entry.Keyword + ShaderKeywordConventions.DECLARED_KEYWORD_SUFFIX, null, false, "1", symbolLocation);
                }
            }
        }

        var shaderApi = myShaderVariantsManager.ShaderApi;
        var shaderApiSymbol = ShaderApiDefineSymbolDescriptor.Instance.GetDefineSymbol(shaderApi);
        yield return new CppPPDefineSymbol(shaderApiSymbol, null, false, "1", new CppSymbolLocation(CppFileLocation.EMPTY, CppComplexOffset.ZERO));
        
        var shaderPlatform = myShaderVariantsManager.ShaderPlatform;
        var shaderPlatformSymbol = ShaderPlatformDefineSymbolDescriptor.Instance.GetDefineSymbol(shaderPlatform);
        yield return new CppPPDefineSymbol(shaderPlatformSymbol, null, false, "1", new CppSymbolLocation(CppFileLocation.EMPTY, CppComplexOffset.ZERO));
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
