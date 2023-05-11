using System;
using System.Linq;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Shaders.HlslSupport.Integration.Cpp
{
    public class UnityHlslDefineSymbolsProviderTest
    {
        [Test]
        public void TestDefineSymbols()
        {
            var unityVersion = new UnityVersionMock(Version.Parse("2021.3.0"));
            var provider = new UnityHlslDefineSymbolsProvider(unityVersion);
            var symbols = provider.GetDefineSymbols().ToDictionary(x => x.Name, x => x);
            Assert.That(symbols.Keys, Is.SupersetOf(new[] { "SHADER_API_D3D11", "__RESHARPER__", "INTERNAL_DATA", "WorldReflectionVector", "WorldNormalVector", "UNITY_VERSION" }));
        }

        [TestCase("5.6.0", "560")]
        [TestCase("2021.3.0", "202130")]
        [TestCase("2021.3.8", "202138")]
        [TestCase("2021.3.21", "202139")]
        public void TestUnityVersion(string versionString, string expectedHlslDefineSymbolValue)
        {
            var unityVersion = new UnityVersionMock(Version.Parse(versionString));
            var provider = new UnityHlslDefineSymbolsProvider(unityVersion);
            var unityVersionDefine = provider.GetDefineSymbols().First(it => it.Name == "UNITY_VERSION");
            Assert.That(unityVersionDefine.Substitution, Is.EqualTo(expectedHlslDefineSymbolValue));
        }
    }
}