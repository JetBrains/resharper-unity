#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Cpp.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public interface IEnabledShaderKeywordsProvider
{
    ISet<string> GetEnabledKeywords(CppFileLocation location);
}