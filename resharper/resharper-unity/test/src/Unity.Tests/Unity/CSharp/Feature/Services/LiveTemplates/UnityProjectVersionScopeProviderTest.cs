using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Feature.Services.LiveTemplates
{
    [TestFixture]
    public class UnityProjectVersionScopeProviderTest : BaseScopeProviderTest
    {
        protected override string RelativeTestDataPath => @"CSharp\LiveTemplates\Scope";
        protected override IScopeProvider CreateScopeProvider() => new UnityProjectVersionScopeProvider();

        [Test, TestUnity(UnityVersion.Unity54)] public void Test54() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity55)] public void Test55() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_1)] public void Test171() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_2)] public void Test172() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_3)] public void Test173() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2017_4)] public void Test174() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2018_1)] public void Test181() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2018_2)] public void Test182() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2018_3)] public void Test183() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2018_4)] public void Test184() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2019_1)] public void Test191() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2019_2)] public void Test192() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2019_3)] public void Test193() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2019_4)] public void Test194() { DoNamedTest(); }
        [Test, TestUnity(UnityVersion.Unity2020_1)] public void Test201() { DoNamedTest(); }
    }
}