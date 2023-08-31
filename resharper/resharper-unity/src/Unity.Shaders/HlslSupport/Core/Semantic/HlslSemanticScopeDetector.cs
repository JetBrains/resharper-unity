#nullable enable
using JetBrains.Application;
using JetBrains.ReSharper.Psi.Cpp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Core.Semantic
{
    [ShellComponent]
    public class HlslSemanticScopeDetector
    {
        public HlslSemanticScope DetectScope(HlslTrailingSemantic semantic)
        {
            return HlslSemanticScope.Unknown;
        }
    }
}