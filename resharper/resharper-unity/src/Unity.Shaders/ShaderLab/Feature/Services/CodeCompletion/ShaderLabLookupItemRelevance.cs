using System;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion
{
    [Flags]
    public enum ShaderLabLookupItemRelevance : ulong
    {
        // same values as in CLRLookupItemRelevance
        LiveTemplates = 1UL << 34,
        Keywords = 1UL << 37
    }
}