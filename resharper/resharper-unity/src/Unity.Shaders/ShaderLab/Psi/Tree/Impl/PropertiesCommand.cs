#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;

internal partial class PropertiesCommand
{
    protected override DeclaredElementType DeclaredElementType => ShaderLabDeclaredElementType.PropertiesCommand;        
}