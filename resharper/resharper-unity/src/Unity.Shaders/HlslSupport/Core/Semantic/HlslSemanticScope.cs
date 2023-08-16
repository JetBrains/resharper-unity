#nullable enable
using System;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic
{
    [Flags]
    public enum HlslSemanticScope
    {
        Unknown = 0,
        VertexInput = 1 << 0,
        FragmentInput = 1 << 1,
        FragmentOutput = 1 << 2,
        VertexOutput = FragmentInput,
        Any = VertexInput | FragmentInput | FragmentOutput
    }
}