#nullable enable
using JetBrains.ReSharper.Psi.Cpp.Language;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;

public sealed record ShaderLabPragmaCommand(string Name, PragmaCommandFlags Flags, ShaderLabPragmaInfo Info) : PragmaCommand(Name, Flags);