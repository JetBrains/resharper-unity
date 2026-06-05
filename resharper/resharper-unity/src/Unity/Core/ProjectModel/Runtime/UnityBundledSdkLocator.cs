#nullable enable
using System.Linq;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Runtime
{
    internal static class UnityBundledSdkLocator
    {
        public static VirtualFileSystemPath GetBundledDotnetFolder(VirtualFileSystemPath unityContentPath)
        {
            if (unityContentPath.IsNullOrEmpty()) return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);

            var context = unityContentPath.InteractionContext;
            var folder = unityContentPath.Combine("Resources/Scripting/DotNetSdk");
            return folder.ExistsDirectory ? folder : VirtualFileSystemPath.GetEmptyPathFor(context);
        }

        public static bool HasBundledSdkWithMsBuild(VirtualFileSystemPath contentPath)
        {
            var folder = GetBundledDotnetFolder(contentPath);
            if (folder.IsEmpty) return false;

            var sdkRoot = folder.Combine("sdk");
            return sdkRoot.ExistsDirectory && sdkRoot.GetChildDirectories().Any(dir => dir.Combine("MSBuild.dll").ExistsFile);
        }
    }
}
