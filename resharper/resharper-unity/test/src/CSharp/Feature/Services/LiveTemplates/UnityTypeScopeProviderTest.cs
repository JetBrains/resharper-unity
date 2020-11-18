using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Tests.Framework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services.LiveTemplates
{
    [TestUnity]
    public class UnityTypeScopeProviderTest : BaseScopeProviderTest
    {
        protected override string RelativeTestDataPath => @"CSharp\LiveTemplates\Scope";
        protected override IScopeProvider CreateScopeProvider() => new UnityTypeScopeProvider();
        
        [Test] public void TestInMonoBehaviour() { DoNamedTest2(); }
        [Test] public void TestInScriptableObject() { DoNamedTest2(); }
        [Test] public void TestInUnityCSharpFile01() { DoNamedTest2(); }
        [Test] public void TestInUnityCSharpFile02() { DoNamedTest2(); }
    }
}