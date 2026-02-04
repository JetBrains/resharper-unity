using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [RequireHlslSupport, TestUnity]
    public class UnityInjectedHlslDefineSymbolsProviderTest : BaseTestWithSingleProject
    {
        protected override string RelativeTestDataPath => @"Shaderlab\Daemon\Hlsl\DefineSymbols";

        [TestCase("201940", "TestUnityVersion.shader")]
        [TestCase("201940", "TestUnityVersionInclude.shader")]
        public void TestUnityVersionInjected(string expectedHlslDefineSymbolValue, string shaderPath) => TestInjectedHlslUnityVersion(expectedHlslDefineSymbolValue, shaderPath);
        
        private void TestInjectedHlslUnityVersion(string expectedHlslDefineSymbolValue, string shaderPath) =>
            TestDefineSymbols(shaderPath, symbols =>
            {
                var unityVersionDefine = symbols.First(it => it.Name == "UNITY_VERSION");
                Assert.That(unityVersionDefine.Substitution, Is.EqualTo(expectedHlslDefineSymbolValue));
            });
        
        private void TestDefineSymbols(string shaderPath, Action<IEnumerable<CppPPDefineSymbol>> action) =>
            DoTestSolution(_ => CreateSolutionConfiguration(new [] {shaderPath}), (_, solution) =>
            {
                var defineSymbols = GetDefineSymbols(solution);
                Assert.NotNull(defineSymbols);
                action(defineSymbols!);
            });
        

        private IEnumerable<CppPPDefineSymbol>? GetDefineSymbols(ISolution solution)
        {
            var project = solution.GetProjectByName("TestProject");
            Assert.NotNull(project);
            var symbolCache = solution.GetComponent<CppGlobalCacheImpl>().SymbolCache;

            var firstProjectFile = project.GetAllProjectFiles().First();
            var firstSourceFile = firstProjectFile.ToSourceFile();
            Assert.NotNull(firstSourceFile);
                
            var location = InjectedHlslLocationHelper.GetCppFileLocations(firstSourceFile).FirstOrDefault();
            
            if (!solution.GetComponent<ShaderProgramCache>().TryGetShaderProgramInfo(location.Location,  out var shaderProgramInfo))
                    Assertion.Fail($"Shader program info is missing for {location}");

            var compilationProperties = solution.GetComponent<UnityHlslCppCompilationPropertiesProvider>().GetShaderLabHlslCompilationProperties(solution, project, location.Location, shaderProgramInfo);
                    
            return compilationProperties?.PredefinedMacros;
        }

        protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId targetFrameworkId)
        {
            return EmptyList<string>.Enumerable;
        }
    }
}