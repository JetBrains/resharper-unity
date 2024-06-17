using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [TestUnity]
    public class UnityHlslDefineSymbolsProviderTest : BaseTestWithSingleProject
    {
        [Test, TestUnity]
        public void TestDefineSymbols() =>
            TestDefineSymbols(defineSymbols =>
            {
                var symbolsMap = defineSymbols.ToDictionary(x => x.Name, x => x);
                Assert.That(symbolsMap.Keys, Is.SupersetOf(new[] { "SHADER_API_D3D11", "UNITY_VERSION" }));    
            });

        [Test, TestUnity(UnityVersion.Unity56)]
        public void Test5_6() => TestUnityVersion("560");
        [Test, TestUnity(2021, 3, 0)]
        public void Test2021_3_0() => TestUnityVersion("202130");
        [Test, TestUnity(2021, 3, 8)]
        public void Test2021_3_8() => TestUnityVersion("202138");
        [Test, TestUnity(2021, 3, 21)]
        public void Test2021_3_21() => TestUnityVersion("202139");

        private void TestDefineSymbols(Action<IEnumerable<CppPPDefineSymbol>> action) =>
            DoTestSolution(_ => CreateSolutionConfiguration(EmptyList<string>.Instance), (_, solution) =>
            {
                var defineSymbols = GetDefineSymbols(solution);
                Assert.NotNull(defineSymbols);
                action(defineSymbols!);
            });

        private void TestUnityVersion(string expectedHlslDefineSymbolValue) =>
            TestDefineSymbols(symbols =>
            {
                var unityVersionDefine = symbols.First(it => it.Name == "UNITY_VERSION");
                Assert.That(unityVersionDefine.Substitution, Is.EqualTo(expectedHlslDefineSymbolValue));
            });

        private IEnumerable<CppPPDefineSymbol>? GetDefineSymbols(ISolution solution)
        {
            var project = solution.GetProjectByName("TestProject");
            Assert.NotNull(project);
            var symbolCache = solution.GetComponent<CppGlobalCacheImpl>().SymbolCache;
            var compilationProperties = solution.GetComponent<UnityHlslCppCompilationPropertiesProvider>().GetCompilationProperties(project, null, new CppFileLocation(VirtualFileSystemPath.Parse("Dummy.hlsl", solution.GetInteractionContext())), symbolCache, null);
            return compilationProperties?.PredefinedMacros;
        }

        protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId targetFrameworkId)
        {
            return EmptyList<string>.Enumerable;
        }
    }
}