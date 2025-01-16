#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.VCXProj;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [CppCompilationPropertiesProvider(Priority = CppCompilationPropertiesProviderAttribute.NORMAL_PRIORITY)]
    public class UnityHlslCppCompilationPropertiesProvider(
        IUnityVersion unityVersion,
        CgIncludeDirectoryProvider cgIncludeDirectoryProvider,
        IImmutableEnumerable<IUnityHlslCustomMacrosProvider> customDefinesProviders,
        ShaderProgramCache shaderProgramCache,
        UnityDialects dialects)
        : ICppCompilationPropertiesProvider
    {
        public CppCompilationProperties? GetCompilationProperties(IProject project, IProjectFile? projectFile, CppFileLocation rootFile,
            CppGlobalSymbolCache globalCache, CppIntelliSenseInfo? intelliSenseInfo)
        {
            if (project.ProjectProperties is VCXProjectProperties || !project.IsUnityProject()) return null;

            return rootFile.Location switch
            {
                var location when UnityShaderFileUtils.IsShaderLabFile(location) =>
                    GetShaderProgramCompilationProperties(globalCache.CppModule, project, projectFile, rootFile, dialects.ShaderLabHlslDialect),
                var location when UnityShaderFileUtils.IsComputeShaderFile(location) => 
                    GetShaderProgramCompilationProperties(globalCache.CppModule, project, projectFile, rootFile, dialects.ComputeHlslDialect),
                var location when PsiSourceFileUtil.IsBlockShadersFile(location) =>
                    GetHlslCompilationProperties(globalCache.Solution, project, rootFile, null, dialects.BlockShadersDialect),
                var location when PsiSourceFileUtil.IsHlslFile(location) => 
                    GetHlslCompilationProperties(globalCache.Solution, project, rootFile, null, dialects.HlslDialect),
                _ => null
            };
        }
        
        private CppCompilationProperties GetShaderProgramCompilationProperties(CppExternalModule module, IProject project, IProjectFile? projectFile, CppFileLocation rootFile, UnityHlslDialectBase dialect)
        {
            var sourceFile = projectFile?.ToSourceFile() ?? rootFile.GetRandomSourceFile(module);
            shaderProgramCache.TryGetOrReadUpToDateProgramInfo(sourceFile, rootFile, out var shaderProgramInfo);
            return GetHlslCompilationProperties(module.GetSolution(), project, rootFile, shaderProgramInfo, dialect);
        }

        public CppCompilationProperties GetShaderLabHlslCompilationProperties(ISolution solution, IProject? project, CppFileLocation location, ShaderProgramInfo shaderProgramInfo) => 
            GetHlslCompilationProperties(solution, project, location, shaderProgramInfo, dialects.ShaderLabHlslDialect);        

        private CppCompilationProperties GetHlslCompilationProperties(ISolution solution, IProject? project, CppFileLocation location, ShaderProgramInfo? shaderProgramInfo, CppLanguageDialect dialect)
        {
            var solutionDirectory = solution.SolutionDirectory;
            
            var properties = new CppCompilationProperties
            {
                OverridenDialect =  dialect,
                LanguageKind = dialect.LanguageKind
            };
            var path = cgIncludeDirectoryProvider.GetCgIncludeFolderPath();
            if (!path.IsEmpty)
                properties.HeaderSearchPaths.IncludePaths.Add(path);
            properties.HeaderSearchPaths.IncludePaths.Add(solutionDirectory);
            
            properties.ForcedIncludes.Add(solutionDirectory.Combine(Utils.ShaderConfigFile).FullPath);
            
            DefineCommonSymbols(project, properties.PredefinedMacros);
            
            // Add macros from custom providers
            foreach (var provider in customDefinesProviders)
                properties.PredefinedMacros.AddRange(provider.ProvideCustomMacros(location, shaderProgramInfo));
            
            // Add macros from shader program info (if provided)
            if (shaderProgramInfo != null)
            {
                if (shaderProgramInfo.ShaderType == ShaderType.Surface)
                    DefineSurfaceShaderSymbols(properties.PredefinedMacros);
                foreach (var (name, value) in shaderProgramInfo.DefinedMacros)
                    properties.PredefinedMacros.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro($"{name}={value}"));
            }
            
            return properties;
        }

        private void DefineCommonSymbols(IProject? project, List<CppPPDefineSymbol> definedSymbols)
        {
            definedSymbols.Add(CppPPDefineSymbolUtil.CreatePredefinedSymbol("UNITY_VERSION", GetHlslUnityVersion(project).ToString()));
        }

        private void DefineSurfaceShaderSymbols(List<CppPPDefineSymbol> definedSymbols)
        {
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("INTERNAL_DATA= "));
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldReflectionVector(data,normal)=data.worldRefl"));
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldNormalVector(data,normal)=normal"));
        }

        private int GetHlslUnityVersion(IProject? project)
        {
            var version = unityVersion.GetActualVersion(project);
            return version.Major * 100 + version.Minor * 10 + version.Build.Clamp(0, 9);
        }
    }
}