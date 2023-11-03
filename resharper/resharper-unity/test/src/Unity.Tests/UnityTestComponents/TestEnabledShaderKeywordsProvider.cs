using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Psi.Cpp.Caches;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent]
    public class TestEnabledShaderKeywordsProvider : IEnabledShaderKeywordsProvider
    {
        public HashSet<string> EnabledKeywords { get; } = new();

        public ISet<string> GetEnabledKeywords(CppFileLocation location) => EnabledKeywords;
    }
}