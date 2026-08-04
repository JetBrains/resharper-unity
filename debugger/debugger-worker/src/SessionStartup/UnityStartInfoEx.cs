using System;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Util;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    public static class UnityStartInfoEx
    {
        // since with the advent of CoreCLR, unity-related start info types might be in different inheritance hierarchies,
        // so we can't simply inherit the project data field, instead we have it in different types that are marked with the same inferface,
        // and to retrieve this field we simply match the specific type here.
        // TODO: we should probably think of a better way to supply additional data to the debugger without relying
        //  on the concrete start info types
        public static UnityProjectData GetProjectData(this UnityStartInfo startInfo)
        {
            return startInfo switch
            {
                UnityMonoStartInfoBase i => i.ProjectData,
                UnityLocalCoreClrStartInfo i => i.ProjectData,
                UnityDotNetCoreExeStartInfo i => i.ProjectData,
                UnityDotNetCoreAttachStartInfo i => i.ProjectData,
                _ => throw new ArgumentException("Unsupported unity start info type"),
            };
        }

        public static UnityBundleInfo? GetBundleInfo(this UnityStartInfo startInfo, string id, ILogger logger)
        {
            var bundleInfo = startInfo.GetProjectData().Bundles.Find(bundle => bundle.Id == id);
            if (bundleInfo == null) logger.Error($"Bundle '{id}' was not found in the Unity plugin.");
            return bundleInfo;
        }
    }
}