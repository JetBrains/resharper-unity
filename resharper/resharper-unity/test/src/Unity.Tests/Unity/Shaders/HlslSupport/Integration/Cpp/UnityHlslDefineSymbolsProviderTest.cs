using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [TestUnity(ProvideReferences = false)]
    public class UnityHlslDefineSymbolsProviderTest : BaseTestWithSingleProject
    {
        [Test, TestUnity(ProvideReferences = false)]
        public void TestDefineSymbols() =>
            DoTestSolution((_, solution) =>
            {
                var symbols = GetSymbolDefines(solution).ToDictionary(x => x.Name, x => x);
                Assert.That(symbols.Keys, Is.SupersetOf(new[] { "SHADER_API_D3D11", "INTERNAL_DATA", "WorldReflectionVector", "WorldNormalVector", "UNITY_VERSION" }));    
            });

        [Test, TestUnity(UnityVersion.Unity56, ProvideReferences = false)]
        public void Test5_6() => TestUnityVersion("560");
        [Test, TestUnity(2021, 3, 0, ProvideReferences = false)]
        public void Test2021_3_0() => TestUnityVersion("202130");
        [Test, TestUnity(2021, 3, 8, ProvideReferences = false)]
        public void Test2021_3_8() => TestUnityVersion("202138");
        [Test, TestUnity(2021, 3, 21, ProvideReferences = false)]
        public void Test2021_3_21() => TestUnityVersion("202139");
        
        private void TestUnityVersion(string expectedHlslDefineSymbolValue) =>
            DoTestSolution(_ => CreateSolutionConfiguration(EmptyList<string>.Instance), (_, solution) =>
            {
                var unityVersionDefine = GetSymbolDefines(solution).First(it => it.Name == "UNITY_VERSION");
                Assert.That(unityVersionDefine.Substitution, Is.EqualTo(expectedHlslDefineSymbolValue));    
            });

        private List<CppPPDefineSymbol> GetSymbolDefines(ISolution solution)
        {
            var project = solution.GetProjectByName("TestProject");
            var defineSymbols = new List<CppPPDefineSymbol>();
            solution.GetComponent<UnityHlslCppCompilationPropertiesProvider>().DefineSymbols(project, defineSymbols);
            return defineSymbols;
        }
    }
}