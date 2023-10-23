#nullable enable
using System;
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

        public UnityHlslDialect HlslDialect { get; } = new();
        public UnityShaderLabHlslDialect ShaderLabHlslDialect { get; } = new();
        public UnityComputeHlslDialect ComputeHlslDialect { get; } = new();

        public UnityHlslCppCompilationPropertiesProvider(IUnityVersion unityVersion, CgIncludeDirectoryProvider cgIncludeDirectoryProvider, IReadOnlyList<IUnityHlslCustomMacrosProvider> customDefinesProviders)
        {
            myUnityVersion = unityVersion;
            myCgIncludeDirectoryProvider = cgIncludeDirectoryProvider;
            myCustomDefinesProviders = customDefinesProviders;
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
                        var shaderProgramInfo = solution.GetComponent<ShaderProgramCache>().GetOrReadUpToDateProgramInfo(sourceFile, rootFile);
                        return GetShaderLabHlslCompilationProperties(solution, rootFile, shaderProgramInfo);
                    }
                    case CppProjectFileType.COMPUTE_EXTENSION:
                        return GetHlslCompilationProperties(solution, ComputeHlslDialect);
                    case var _ when PsiSourceFileUtil.IsHlslFile(filePath):
                        return GetHlslCompilationProperties(solution, HlslDialect);
                }
            }

            return null;
        }

        public CppCompilationProperties GetShaderLabHlslCompilationProperties(ISolution solution, CppFileLocation location, ShaderProgramInfo shaderProgramInfo)
        {
            var properties = GetHlslCompilationProperties(solution, ShaderLabHlslDialect);
            if (shaderProgramInfo.ShaderType == ShaderType.Surface)
                DefineSurfaceShaderSymbols(properties.PredefinedMacros);
            foreach (var (name, value) in shaderProgramInfo.DefinedMacros) 
                properties.PredefinedMacros.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro($"{name}={value}"));
            foreach (var provider in myCustomDefinesProviders)
                properties.PredefinedMacros.AddRange(provider.ProvideCustomMacros(location, shaderProgramInfo));
            return properties;
        }

        private CppCompilationProperties GetHlslCompilationProperties(ISolution solution, UnityHlslDialectBase dialect)
        {
            var properties = new CppCompilationProperties
            {
                OverridenDialect =  dialect,
                LanguageKind = dialect.LanguageKind
            };
            var path = myCgIncludeDirectoryProvider.GetCgIncludeFolderPath();
            if (!path.IsEmpty)
                properties.HeaderSearchPaths.IncludePaths.Add(path);

            var solutionDirectory = solution.SolutionDirectory;
            properties.ForcedIncludes.Add(solutionDirectory.Combine(Utils.ShaderConfigFile).FullPath);
            DefineCommonSymbols(properties.PredefinedMacros);
            
            properties.HeaderSearchPaths.IncludePaths.Add(solutionDirectory);
            return properties;
        }

        private void DefineCommonSymbols(List<CppPPDefineSymbol> definedSymbols)
        {
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("SHADER_API_D3D11"));
            definedSymbols.Add(CppPPDefineSymbolUtil.CreatePredefinedSymbol("UNITY_VERSION", GetHlslUnityVersion().ToString()));
        }

        private void DefineSurfaceShaderSymbols(List<CppPPDefineSymbol> definedSymbols)
        {
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("INTERNAL_DATA= "));
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldReflectionVector(data,normal)=data.worldRefl"));
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldNormalVector(data,normal)=normal"));
        }

        public void DefineSymbols(List<CppPPDefineSymbol> definedSymbols)
        {
            DefineCommonSymbols(definedSymbols);
            DefineSurfaceShaderSymbols(definedSymbols);
        }

        private int GetHlslUnityVersion()
        {
            var version = myUnityVersion.ActualVersionForSolution.Maybe.ValueOrDefault;
            return version != null ? version.Major * 1_00 + version.Minor * 10 + Math.Min(version.Build, 9) : 0;
        }
    }
}