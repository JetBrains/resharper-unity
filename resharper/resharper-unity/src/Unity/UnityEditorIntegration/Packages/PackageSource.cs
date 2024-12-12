namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages
{
    public enum PackageSource
    {
        Unknown,
        BuiltIn,
        Registry,
        Embedded,
        Local,
        LocalTarball,
        Git
    }

    public static class PackageSourceExtensions
    {
        public static PackageSource ToPackageSource(string source)
        {
            if (string.IsNullOrEmpty(source)) return PackageSource.Unknown;
            return source switch
            {
                "embedded" => PackageSource.Embedded,
                "registry" => PackageSource.Registry,
                "builtin" => PackageSource.BuiltIn,
                "git" => PackageSource.Git,
                "local" => PackageSource.Local,
                "local-tarball" => PackageSource.LocalTarball,
                _ => PackageSource.Unknown
            };
        }
    }
}