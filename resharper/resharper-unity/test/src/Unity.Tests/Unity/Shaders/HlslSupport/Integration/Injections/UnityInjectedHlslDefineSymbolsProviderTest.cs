using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Shaders.HlslSupport.Integration.Injections
{
    [RequireHlslSupport, TestUnity]
    public class UnityInjectedHlslDefineSymbolsProviderTest : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath => @"Shaders\HlslSupport\Integration\Injections";
        
        private void TestUnityVersionInjected(string expectedHlslDefineSymbolValue) => TestInjectedHlslUnityVersion(expectedHlslDefineSymbolValue);

        
        [Test, TestUnity(UnityVersion.Unity56)]
        public void Test5_6() => TestUnityVersionInjected("560");
        [Test, TestUnity(2021, 3, 0)]
        public void Test2021_3_0() => TestUnityVersionInjected("202130");
        [Test, TestUnity(2021, 3, 8)]
        public void Test2021_3_8() => TestUnityVersionInjected("202138");
        [Test, TestUnity(2021, 3, 21)]
        public void Test2021_3_21() => TestUnityVersionInjected("202139");
        
        private void TestInjectedHlslUnityVersion(string expectedHlslDefineSymbolValue) =>
            TestDefineSymbol(symbol =>
            {
                Assert.That(symbol.Substitution, Is.EqualTo(expectedHlslDefineSymbolValue));
            });
        
        private void TestDefineSymbol(Action<CppPPDefineSymbol> action) =>
            DoTestSolution(_ => CreateSolutionConfiguration(new [] {"TestUnityVersion.shader"}), (_, solution) =>
            {
                var defineSymbols = GetDefineSymbol(solution);
                Assert.NotNull(defineSymbols);
                action(defineSymbols);
            });
        

        private CppPPDefineSymbol GetDefineSymbol(ISolution solution)
        {
            var project = solution.GetProjectByName("TestProject");
            Assert.NotNull(project);
            var symbolCache = solution.GetComponent<CppGlobalCacheImpl>().SymbolCache;

            var firstProjectFile = project.GetAllProjectFiles().First();
            var firstSourceFile = firstProjectFile.ToSourceFile();
            Assert.NotNull(firstSourceFile);
                
            var location = InjectedHlslLocationHelper.GetCppFileLocations(firstSourceFile).FirstOrDefault();
            Assert.NotNull(location.Location);
            
            var inclusionContext = CppInclusionContextUtil.CreateInclusionContext(symbolCache, location.Location, firstProjectFile, solution.GetSolutionLifetimes().UntilSolutionCloseLifetime);
                    
            return inclusionContext.GetDefinition(new CppDefineKey("UNITY_VERSION"));
        }

        protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId targetFrameworkId)
        {
            return EmptyList<string>.Enumerable;
        }
    }
}