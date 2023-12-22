#nullable enable
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Symbols;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;

public interface IUnityHlslCustomMacrosProvider
{
    IEnumerable<CppPPDefineSymbol> ProvideCustomMacros(CppFileLocation location, ShaderProgramInfo? shaderProgramInfo);
}