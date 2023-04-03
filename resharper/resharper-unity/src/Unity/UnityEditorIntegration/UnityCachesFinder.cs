using System;
using JetBrains.Annotations;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    public static class UnityCachesFinder
    {
        [CanBeNull]
        public static VirtualFileSystemPath GetPackagesCacheFolder(string registry)
        {
            const string defaultRegistryHost = "packages.unity.com";

            var cacheRoot = GetPackagesCacheRoot();

            var registryHost = defaultRegistryHost;
            if (Uri.TryCreate(registry, UriKind.Absolute, out var registryUri))
            {
                registryHost = registryUri.Host;
            }

            var cacheFolder = cacheRoot.Combine(registryHost);
            if (!cacheFolder.ExistsDirectory)
                cacheFolder = cacheRoot.Combine(defaultRegistryHost);

            return cacheFolder.ExistsDirectory ? cacheFolder : null;
        }

        [NotNull]
        public static VirtualFileSystemPath GetPackagesCacheRoot()
        {
            var upmCachePath = Environment.GetEnvironmentVariable("UPM_CACHE_PATH");
            if (!string.IsNullOrEmpty(upmCachePath))
                return VirtualFileSystemPath.Parse(upmCachePath, InteractionContext.SolutionContext);

            switch (PlatformUtil.RuntimePlatform)
            {
                case JetPlatform.Windows:
                    return Environment.SpecialFolder.LocalApplicationData.ToVirtualFileSystemPath(InteractionContext.SolutionContext).Combine("Unity/cache/packages");
                case JetPlatform.MacOsX:
                    return Environment.SpecialFolder.Personal.ToVirtualFileSystemPath(InteractionContext.SolutionContext).Combine("Library/Unity/cache/packages");
                case JetPlatform.Linux:
                    // This will check $XDG_CONFIG_HOME, if it exists, and fall back to ~/.config
                    // TODO: Check this works
                    return Environment.SpecialFolder.ApplicationData.ToVirtualFileSystemPath(InteractionContext.SolutionContext).Combine("unity3d/cache/packages");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        [NotNull]
        public static VirtualFileSystemPath GetLocalPackageCacheFolder(VirtualFileSystemPath solutionDirectory)
        {
            return solutionDirectory.Combine("Library/PackageCache");
        }
        
        // TODO: We shouldn't have to pass in appPath here
        // But appPath is being calculated by UnityVersion, not UnityInstallationFinder
        [NotNull]
        public static VirtualFileSystemPath GetBuiltInPackagesFolder([NotNull] VirtualFileSystemPath applicationPath)
        {
            return applicationPath.IsEmpty
                ? applicationPath
                : UnityInstallationFinder.GetApplicationContentsPath(applicationPath).Combine("Resources/PackageManager/BuiltInPackages");
        }
    }

}