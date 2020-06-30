using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
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

                return properties;
            }

            return null;
        }
    }
}