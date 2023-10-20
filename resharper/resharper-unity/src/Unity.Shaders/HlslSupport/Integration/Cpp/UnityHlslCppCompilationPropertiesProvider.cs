#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Language;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
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
        private readonly IReadOnlyList<IUnityHlslCustomDefinesProvider> myCustomDefinesProviders;

        public UnityHlslDialect HlslDialect { get; } = new();
        public UnityShaderLabHlslDialect ShaderLabHlslDialect { get; } = new();
        public UnityComputeHlslDialect ComputeHlslDialect { get; } = new();

        public UnityHlslCppCompilationPropertiesProvider(IUnityVersion unityVersion, CgIncludeDirectoryProvider cgIncludeDirectoryProvider, IReadOnlyList<IUnityHlslCustomDefinesProvider> customDefinesProviders)
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
                        return GetShaderLabHlslCompilationProperties(solution);
                    case CppProjectFileType.COMPUTE_EXTENSION:
                        return GetHlslCompilationProperties(solution, ComputeHlslDialect);
                    case var _ when PsiSourceFileUtil.IsHlslFile(filePath):
                        return GetHlslCompilationProperties(solution, HlslDialect);
                }
            }

            return null;
        }

        public CppCompilationProperties GetShaderLabHlslCompilationProperties(ISolution solution)
        {
            var properties = GetHlslCompilationProperties(solution, ShaderLabHlslDialect);
            DefineSurfaceShaderSymbols(properties.PredefinedMacros);
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
            DefineCommonSymbols(properties.PredefinedMacros, dialect);
            
            properties.HeaderSearchPaths.IncludePaths.Add(solutionDirectory);
            return properties;
        }

        private void DefineCommonSymbols(List<CppPPDefineSymbol> definedSymbols, UnityHlslDialectBase dialect)
        {
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("SHADER_API_D3D11"));
            definedSymbols.Add(CppPPDefineSymbolUtil.CreatePredefinedSymbol("UNITY_VERSION", GetHlslUnityVersion().ToString()));
            foreach (var provider in myCustomDefinesProviders)
                definedSymbols.AddRange(provider.ProvideCustomDefines(dialect).Select(x => CppPPDefineSymbolUtil.CreatePredefinedSymbol(x, "1")));
        }

        private void DefineSurfaceShaderSymbols(List<CppPPDefineSymbol> definedSymbols)
        {
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("INTERNAL_DATA= "));
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldReflectionVector(data,normal)=data.worldRefl"));
            definedSymbols.Add(CppPPDefineSymbolUtil.ParsePredefinedMacro("WorldNormalVector(data,normal)=normal"));
        }

        public void DefineSymbols(List<CppPPDefineSymbol> definedSymbols, UnityHlslDialectBase dialect)
        {
            DefineCommonSymbols(definedSymbols, dialect);
            DefineSurfaceShaderSymbols(definedSymbols);
        }

        private int GetHlslUnityVersion()
        {
            var version = myUnityVersion.ActualVersionForSolution.Maybe.ValueOrDefault;
            return version != null ? version.Major * 1_00 + version.Minor * 10 + Math.Min(version.Build, 9) : 0;
        }
    }
}