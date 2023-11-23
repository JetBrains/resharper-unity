#nullable enable
using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class UnityHlslCppCompilationPropertiesProvider : ICppCompilationPropertiesProvider
    {
        private readonly IUnityVersion myUnityVersion;
        private readonly CgIncludeDirectoryProvider myCgIncludeDirectoryProvider;
        private readonly IReadOnlyList<IUnityHlslCustomMacrosProvider> myCustomDefinesProviders;
        private readonly ShaderProgramCache myShaderProgramCache;
        private readonly UnityDialects myDialects;

        public UnityHlslCppCompilationPropertiesProvider(IUnityVersion unityVersion, CgIncludeDirectoryProvider cgIncludeDirectoryProvider, IReadOnlyList<IUnityHlslCustomMacrosProvider> customDefinesProviders, ShaderProgramCache shaderProgramCache, UnityDialects dialects)
        {
            myUnityVersion = unityVersion;
            myCgIncludeDirectoryProvider = cgIncludeDirectoryProvider;
            myCustomDefinesProviders = customDefinesProviders;
            myShaderProgramCache = shaderProgramCache;
            myDialects = dialects;
        }

        public CppCompilationProperties? GetCompilationProperties(IProject project, IProjectFile? projectFile, CppFileLocation rootFile,
            CppGlobalSymbolCache globalCache)
        {
            if (project.IsUnityProject())
            {
                var filePath = rootFile.Location;
                var solution = globalCache.Solution;
                switch (filePath.ExtensionWithDot)
                {
                    case ShaderLabProjectFileType.SHADERLAB_EXTENSION:
                    {
                        var sourceFile = projectFile?.ToSourceFile() ?? rootFile.GetRandomSourceFile(solution);
                        if (myShaderProgramCache.TryGetOrReadUpToDateProgramInfo(sourceFile, rootFile, out var shaderProgramInfo))
                            return GetShaderLabHlslCompilationProperties(solution, project, rootFile, shaderProgramInfo);
                        break;
                    }
                    case CppProjectFileType.COMPUTE_EXTENSION:
                        return GetHlslCompilationProperties(solution, project, rootFile, null, myDialects.ComputeHlslDialect);
                    case var _ when PsiSourceFileUtil.IsHlslFile(filePath):
                        return GetHlslCompilationProperties(solution, project, rootFile, null, myDialects.HlslDialect);
                }
            }

            return null;
        }

        public CppCompilationProperties GetShaderLabHlslCompilationProperties(ISolution solution, IProject? project, CppFileLocation location, ShaderProgramInfo shaderProgramInfo) => 
            GetHlslCompilationProperties(solution, project, location, shaderProgramInfo, myDialects.ShaderLabHlslDialect);

        private CppCompilationProperties GetHlslCompilationProperties(ISolution solution, IProject? project, CppFileLocation location, ShaderProgramInfo? shaderProgramInfo, UnityHlslDialectBase dialect)
        {
            var solutionDirectory = solution.SolutionDirectory;
            
            var properties = new CppCompilationProperties
            {
                OverridenDialect =  dialect,
                LanguageKind = dialect.LanguageKind
            };
            var path = myCgIncludeDirectoryProvider.GetCgIncludeFolderPath();
            if (!path.IsEmpty)
                properties.HeaderSearchPaths.IncludePaths.Add(path);
            properties.HeaderSearchPaths.IncludePaths.Add(solutionDirectory);
            
            properties.ForcedIncludes.Add(solutionDirectory.Combine(Utils.ShaderConfigFile).FullPath);
            
            DefineCommonSymbols(project, properties.PredefinedMacros);
            
            // Add macros from custom providers
            foreach (var provider in myCustomDefinesProviders)
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
            var version = myUnityVersion.GetActualVersion(project);
            return version.Major * 100 + version.Minor * 10 + version.Build.Clamp(0, 9);
        }
    }
}