using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.LiveTemplates
{
    [TestUnity]
    public class UnityTypeScopeProviderTest : BaseScopeProviderTest
    {
        protected override string RelativeTestDataPath => @"CSharp\LiveTemplates\Scope";
        protected override IScopeProvider CreateScopeProvider() => new UnityTypeScopeProvider();

        [Test, UnityPluginBackendChecklist("Live templates", "Unity type scope in C#")] public void TestInMonoBehaviour() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Live templates", "Unity type scope in C#")] public void TestInScriptableObject() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Live templates", "Unity type scope in C#")] public void TestInUnityCSharpFile01() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Live templates", "Unity type scope in C#")] public void TestInUnityCSharpFile02() { DoNamedTest2(); }
    }
}