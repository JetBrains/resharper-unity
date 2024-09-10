#nullable enable
using System;
using System.Collections.Immutable;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using NuGet;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public class UrtCompilationModeDefineSymbolDescriptor : IShaderDefineSymbolDescriptor
{
    public static readonly UrtCompilationModeDefineSymbolDescriptor Instance = new();
    
    private const string Compute = "UNIFIED_RT_BACKEND_COMPUTE";
    private const string Hardware = "UNIFIED_RT_BACKEND_HARDWARE";

    public ImmutableArray<string> AllSymbols { get; } = ImmutableArray.Create<string>(Compute, Hardware);

    public const UrtCompilationMode DefaultValue = UrtCompilationMode.Compute;

    public bool IsDefaultSymbol(string defineSymbol) => defineSymbol == Compute;
    public bool IsApplicable(CppFileLocation location) => UnityShaderFileUtils.IsUrtShaderFile(location);

    public string GetDefineSymbol(UrtCompilationMode urtMode) =>
        urtMode switch
        {
            UrtCompilationMode.Compute => Compute,
            UrtCompilationMode.Hardware => Hardware,
            _ => throw new ArgumentOutOfRangeException(nameof(urtMode), urtMode, null)
        };

    public UrtCompilationMode GetValue(string defineSymbol) =>
        defineSymbol switch
        {
            Compute => UrtCompilationMode.Compute,
            Hardware => UrtCompilationMode.Hardware,
            _ => throw new ArgumentOutOfRangeException(nameof(defineSymbol), defineSymbol, null)
        };
}