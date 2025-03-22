#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Symbols;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;

[DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
public interface IUnityHlslCustomMacrosProvider
{
    IEnumerable<CppPPDefineSymbol> ProvideCustomMacros(CppFileLocation location, ShaderProgramInfo? shaderProgramInfo);
}