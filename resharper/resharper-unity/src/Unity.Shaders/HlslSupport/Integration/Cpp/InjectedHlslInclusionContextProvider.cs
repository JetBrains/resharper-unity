#nullable enable
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [Language(typeof(ShaderLabLanguage))]
    public class InjectedHlslInclusionContextProvider : IInclusionContextProvider
    {
        public CppInclusionContextResult CreateInclusionContextResult(CppGlobalSymbolCache cache, CppFileLocation rootFile,
            FileProcessingOptions options, long cacheVersion, Lifetime lifetime)
        {
            var locationTracker = cache.Solution.GetComponent<InjectedHlslFileLocationTracker>();
            if (!locationTracker.IsValid(rootFile))
                return CppInclusionContextResult.Fail(CppInclusionContextResult.Status.UNSUITABLE_PROJECT_FILE);

            var compilationPropertiesProvider = cache.Solution.GetComponent<UnityHlslCppCompilationPropertiesProvider>();
            var properties = compilationPropertiesProvider.GetShaderLabHlslCompilationProperties(cache.Solution);
            return CreateInclusionContextResult(cache, rootFile, options, properties, null, cacheVersion, lifetime);
        }
        
        private static CppInclusionContextResult CreateInclusionContextResult(
            CppGlobalSymbolCache cache,
            CppFileLocation rootFile,
            FileProcessingOptions options,
            CppCompilationProperties compilationProperties,
            ISymbolScope? symbolScope,
            long cacheVersion,
            Lifetime lifetime)
        {
            var languageDialect = CppProjectConfigurationUtil.GetLanguageDialect(compilationProperties);
            var randomProjectFile = rootFile.GetRandomProjectFile(cache.Solution);
            var inclusionContext = CppRootInclusionContext.Create(compilationProperties, randomProjectFile.GetProject(),
                randomProjectFile, cache, rootFile, options.File, languageDialect, 
                cacheVersion, options.AllowPendingActions, options.CollectPPUsages, lifetime, symbolScope);
            var directory = randomProjectFile.Location.Directory;

            var (includes, defines) = GetProgramInfo(cache.Solution, rootFile);
            foreach (var define in defines)
            {
                inclusionContext.ProcessDefine(CppPPDefineSymbolUtil.ParsePredefinedMacro($"{define.Key}={define.Value}"));
            }
            
            // TODO 1) is cache ready? what will happen under document transaction? check for bad moment?
            // TODO 2) what will happen under psi transaction? include in cache could be out-of date. Try use include quickfix when cginclude is after cgprogram where QF is used            
            inclusionContext.PushInclude(rootFile, directory, false);
            foreach (var includeLocation in includes)
            {
                if (includeLocation.IsValid() && !includeLocation.Equals(rootFile))
                {
                    cache.LookupAndProcessTableForFile(rootFile, includeLocation, options, inclusionContext, directory);
                }
            }
            inclusionContext.PopInclude(false);
            return CppInclusionContextResult.Ok(inclusionContext);
        }
        
        private static (IEnumerable<CppFileLocation> includeLocations, IReadOnlyDictionary<string, string> defines) GetProgramInfo(ISolution solution, CppFileLocation cppFileLocation)
        {
            var injectedHlslCache = solution.GetComponent<InjectedHlslFileLocationTracker>();
            var shaderProgramCache = solution.GetComponent<ShaderProgramCache>();
            
            // PSI is not committed here
            // TODO: cpp global cache should calculate cache only when PSI for file with cpp injects is committed.

            var sourceFile = cppFileLocation.GetRandomSourceFile(solution);
            var range = cppFileLocation.RootRange;
            Assertion.Assert(range.IsValid);
            
            var buffer = sourceFile.Document.Buffer;

            ShaderProgramInfo? shaderProgramInfo;
            if (!shaderProgramCache.UpToDate(sourceFile))
                shaderProgramInfo = shaderProgramCache.ReadProgramInfo(new CppDocumentBuffer(buffer, range));
            else if (!shaderProgramCache.TryGetShaderProgramInfo(cppFileLocation, out shaderProgramInfo)) 
                Assertion.Fail($"Shader program info is missing for {cppFileLocation}");
            
            var includes = injectedHlslCache.GetIncludes(sourceFile, buffer, range.StartOffset, shaderProgramInfo);
            return (includes, shaderProgramInfo.DefinedMacros);
        }
    }
}