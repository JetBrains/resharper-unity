#nullable enable
using System;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Shaders.HlslSupport.ShaderVariants;

public static class RdUrtCompilationModeEx
{
    public static UrtCompilationMode AsUrtCompilationMode(this RdUrtCompilationMode value) => value switch
    {
        RdUrtCompilationMode.Compute => UrtCompilationMode.Compute,
        RdUrtCompilationMode.Hardware => UrtCompilationMode.Hardware,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
    
    public static RdUrtCompilationMode AsRdUrtCompilationMode(this UrtCompilationMode value) => value switch
    {
        UrtCompilationMode.Compute => RdUrtCompilationMode.Compute,
        UrtCompilationMode.Hardware => RdUrtCompilationMode.Hardware,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}