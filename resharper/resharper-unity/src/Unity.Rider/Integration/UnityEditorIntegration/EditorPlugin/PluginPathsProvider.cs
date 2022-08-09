using System.IO;
using System.Reflection;
using JetBrains.Application.Environment;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.EditorPlugin
{
    [SolutionComponent]
    public class PluginPathsProvider
    {
        private readonly ApplicationPackages myApplicationPackages;
        private readonly IDeployedPackagesExpandLocationResolver myResolver;
        private readonly FileSystemPath myRootDir;
        
        public static readonly string BasicPluginDllFile = "JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll";
        public static readonly string Unity56PluginDllFile = "JetBrains.Rider.Unity.Editor.Plugin.Unity56.Repacked.dll";
        public static readonly string FullPluginDllFile = "JetBrains.Rider.Unity.Editor.Plugin.Full.Repacked.dll";

        public PluginPathsProvider(ApplicationPackages applicationPackages, IDeployedPackagesExpandLocationResolver resolver)
        {
            // System.Diagnostics.Debugger.Launch();

            myApplicationPackages = applicationPackages;
            myResolver = resolver;
            myRootDir = GetRootDir();
        }
        
        public VirtualFileSystemPath GetEditorPluginPathDir()
        {
            var editorPluginPathDir = myRootDir.Combine(@"EditorPlugin");
            return editorPluginPathDir.ToVirtualFileSystemPath();
        }


        public bool ShouldRunInstallation()
        {
            // Avoid EditorPlugin installation when we run from the dotnet-products repository.
            // For the dotnet-products repository EditorPlugin may not be compiled, it is optional
            return !myRootDir.Combine("Product.Root").ExistsFile;
        }

        private FileSystemPath GetRootDir()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var package = myApplicationPackages.FindPackageWithAssembly(assembly, OnError.LogException);
            var installDirectory = myResolver.GetDeployedPackageDirectory(package);
            var root = installDirectory.Parent;
            return root;
        }
    }
}