using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class UnityHlslCppCompilationPropertiesProvider : ICppCompilationPropertiesProvider
    {
        private readonly UnityVersion myUnityVersion;

        public UnityHlslCppCompilationPropertiesProvider(UnityVersion unityVersion)
        {
            myUnityVersion = unityVersion;
        }
        
        public CppCompilationProperties GetCompilationProperties(IProject project, IProjectFile projectFile, CppFileLocation rootFile,
            CppGlobalSymbolCache globalCache)
        {
            if (project.IsUnityProject() && CppProjectFileType.ALL_HLSL_EXTENSIONS.Contains(rootFile.Location.ExtensionWithDot))
            {
                var properties = new CppCompilationProperties();
                properties.LanguageKind = CppLanguageKind.HLSL;
                var path = CgIncludeDirectoryTracker.GetCgIncludeFolderPath(myUnityVersion);
                if (!path.IsEmpty)
                    properties.IncludePaths.Add(path);

                properties.ForcedIncludes.Add(globalCache.Solution.SolutionDirectory.Combine(Utils.ShaderConfigFile).FullPath);

                properties.PredefinedMacros.Add(CppPPDefineSymbol.ParsePredefinedMacro("SHADER_API_D3D11"));
                properties.IncludePaths.Add(globalCache.Solution.SolutionDirectory);
                properties.IncludePaths.Add(globalCache.Solution.SolutionDirectory.Combine("Library"));

                return properties;
            }

            return null;
        }
    }
}