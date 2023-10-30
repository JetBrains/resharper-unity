using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Shaders.HlslSupport.Integration.Cpp
{
    public class UnityHlslDefineSymbolsProviderTest
    {
        [Test]
        public void TestDefineSymbols()
        {
            var unityVersion = new UnityVersionMock(Version.Parse("2021.3.0"));
            var symbols = GetSymbolDefines(unityVersion).ToDictionary(x => x.Name, x => x);
            Assert.That(symbols.Keys, Is.SupersetOf(new[] { "SHADER_API_D3D11", "INTERNAL_DATA", "WorldReflectionVector", "WorldNormalVector", "UNITY_VERSION" }));
        }

        [TestCase("5.6.0", "560")]
        [TestCase("2021.3.0", "202130")]
        [TestCase("2021.3.8", "202138")]
        [TestCase("2021.3.21", "202139")]
        public void TestUnityVersion(string versionString, string expectedHlslDefineSymbolValue)
        {
            var unityVersion = new UnityVersionMock(Version.Parse(versionString));
            var unityVersionDefine = GetSymbolDefines(unityVersion).First(it => it.Name == "UNITY_VERSION");
            Assert.That(unityVersionDefine.Substitution, Is.EqualTo(expectedHlslDefineSymbolValue));
        }

        private List<CppPPDefineSymbol> GetSymbolDefines(IUnityVersion unityVersion)
        {
            var defineSymbols = new List<CppPPDefineSymbol>();
            var provider = new UnityHlslCppCompilationPropertiesProvider(unityVersion, new CgIncludeDirectoryProviderStub(unityVersion), EmptyList<IUnityHlslCustomDefinesProvider>.Instance);
            provider.DefineSymbols(defineSymbols, new UnityShaderLabHlslDialect());
            return defineSymbols;
        }
    }
}