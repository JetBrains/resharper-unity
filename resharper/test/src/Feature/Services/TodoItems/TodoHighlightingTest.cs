using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Feature.Services.TodoItems
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class TodoHighlightingTest : ClrTodoHighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => ShaderLabLanguage.Instance;

        [Test] public void TestTodo01() { DoNamedTest2(); }
    }
}