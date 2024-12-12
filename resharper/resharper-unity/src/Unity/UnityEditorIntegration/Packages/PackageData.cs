#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages
{
    public class PackageData
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<PackageData>();
        
        public readonly string Id;
        public readonly VirtualFileSystemPath? PackageFolder;
        public readonly DateTime PackageJsonTimestamp;
        public readonly PackageDetails PackageDetails;
        public readonly PackageSource Source;
        public readonly GitDetails? GitDetails;
        public readonly VirtualFileSystemPath? TarballLocation;

        public PackageData(string id,
                           VirtualFileSystemPath? packageFolder,
                           DateTime packageJsonTimestamp,
                           PackageDetails packageDetails,
                           PackageSource source,
                           GitDetails? gitDetails,
                           VirtualFileSystemPath? tarballLocation)
        {
            Id = id;
            PackageFolder = packageFolder;
            PackageJsonTimestamp = packageJsonTimestamp;
            PackageDetails = packageDetails;
            Source = source;
            GitDetails = gitDetails;
            TarballLocation = tarballLocation;
        }

        // Even if project generation is enabled, not all packages are intended to be user editable
        public bool IsUserEditable => Source is PackageSource.Embedded or PackageSource.Local;

        public static PackageData CreateUnknown(string id, string version,
                                                PackageSource packageSource = PackageSource.Unknown)
        {
            ourLogger.Info($"Creation of unknown package {nameof(id)}:{id}, {nameof(version)}:{version}, {nameof(packageSource)}:{packageSource}");
            
            return new PackageData(id, null, DateTime.MinValue,
                new PackageDetails(id, $"{id}@{version}", version,
                    $"Cannot resolve package '{id}' with version '{version}'",
                    null,
                    new Dictionary<string, string>()), packageSource, null, null);
        }
        
        internal static PackageData? GetFromFolder(string? id,
            VirtualFileSystemPath packageFolder,
            PackageSource packageSource,
            GitDetails? gitDetails = null,
            VirtualFileSystemPath? tarballLocation = null)
        {
            if (packageFolder.ExistsDirectory)
            {
                var packageJsonFile = packageFolder.Combine("package.json");
                if (packageJsonFile.ExistsFile)
                {
                    try
                    {
                        var packageJson = PackageJson.FromJson(packageJsonFile.ReadAllText2().Text);
                        var packageDetails = PackageDetails.FromPackageJson(packageJson, packageFolder);
                        return new PackageData(id ?? packageDetails.CanonicalName, packageFolder,
                            packageJsonFile.FileModificationTimeUtc, packageDetails, packageSource, gitDetails,
                            tarballLocation);
                    }
                    catch (Exception e)
                    {
                        ourLogger.LogExceptionSilently(e);
                        return null;
                    }
                }
            }

            return null;
        }
    }

    public class PackageDetails
    {
        // Note that canonical name is the name field from package.json. It is the truth about the name of the package.
        // The id field in PackageData is the ID used to reference the package in manifest.json or packages-lock.json.
        // The assumption is that these values are always the same.
        public readonly string CanonicalName;
        public readonly string DisplayName;
        public readonly string Version;
        public readonly string? Description;
        public readonly string? DocumentationUrl;
        // [CanBeNull] public readonly string Author;  // Author might actually be a dictionary
        public readonly Dictionary<string, string> Dependencies;

        public PackageDetails(string canonicalName,
                              string displayName,
                              string version,
                              string? description, 
                              string? documentationUrl,
                              Dictionary<string, string> dependencies)
        {
            CanonicalName = canonicalName;
            DisplayName = displayName;
            Version = version;
            Description = description;
            Dependencies = dependencies;
            DocumentationUrl = documentationUrl;
        }

        internal static PackageDetails FromPackageJson(PackageJson? packageJson, VirtualFileSystemPath packageFolder)
        {
            var name = packageJson?.Name ?? packageFolder.Name;
            return new PackageDetails(name, packageJson?.DisplayName ?? name, packageJson?.Version ?? string.Empty,
                packageJson?.Description, packageJson?.DocumentationUrl, packageJson?.Dependencies ?? new Dictionary<string, string>());
        }
    }

    public class GitDetails
    {
        public readonly string Url;
        public readonly string? Hash;
        public readonly string? Revision;

        public GitDetails(string url, string? hash, string? revision)
        {
            Url = url;
            Hash = hash;
            Revision = revision;
        }
    }

    // packages-lock.json (note the 's', this isn't NPM's package-lock.json)
    // This file was introduced in Unity 2019.4 and is a complete list of all packages, including dependencies and
    // transitive dependencies. Versions are fully resolved from manifest.json, fixing conflicts, and also handling the
    // editor minimum version levels. If also contains the appropriate hashes for git and local tarball packages
    // By observation:
    // * `source` can be `builtin`, `registry`, `embedded`, `git`, `local` and `localTarball`
    // * `version` is a semver value for `builtin` and `registry`, a `file:` url for `embedded` and a url for `git`
    //    TODO: document local
    // * `url` is only available for registry packages, and is the url of the registry, e.g. https://packages.unity.com
    // * `hash` is the commit hash for git packages
    // * `dependencies` is a map of package name to version
    // * `depth` is unknown, but seems to be an indicator of a transitive dependency rather than a direct dependency.
    //    E.g. a package only used as a dependency of another package can have a depth of 1, while the parent package
    //    has a depth of 0
    internal class PackagesLockJson
    {
        public readonly Dictionary<string, PackagesLockDependency> Dependencies;

        [JsonConstructor]
        private PackagesLockJson(Dictionary<string, PackagesLockDependency> dependencies)
        {
            Dependencies = dependencies;
        }

        public static PackagesLockJson? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PackagesLockJson>(json);
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    internal class PackagesLockDependency
    {
        public readonly string Version;
        public readonly int? Depth;
        public readonly string? Source;
        public readonly Dictionary<string, string> Dependencies;
        public readonly string? Url;
        public readonly string? Hash;

        public PackagesLockDependency(string version, int? depth, string? source,
                                      Dictionary<string, string> dependencies, string? url,
                                      string? hash)
        {
            Version = version;
            Depth = depth;
            Source = source;
            Dependencies = dependencies;
            Url = url;
            Hash = hash;
        }
    }

    internal class PackageJson
    {
        public readonly string? Name;
        public readonly string? DisplayName;
        public readonly string? Version;
        public readonly string? Description;
        public readonly string? DocumentationUrl;
        // public readonly string? Author; // TODO: Author might be a map<string, string>, e.g. author[name]
        public readonly Dictionary<string, string> Dependencies;

        [JsonConstructor]
        private PackageJson(string? name,
                            string? displayName,
                            string? version,
                            string? description,
                            string? documentationUrl,
                            Dictionary<string, string>? dependencies)
        {
            Name = name;
            DisplayName = displayName;
            Version = version;
            Description = description;
            DocumentationUrl = documentationUrl;
            Dependencies = dependencies ?? new Dictionary<string, string>();
        }

        public static PackageJson? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PackageJson>(json);
        }
    }

    internal class ManifestJson
    {
        public readonly IDictionary<string, string> Dependencies;
        public readonly string? Registry;
        public readonly IDictionary<string, ManifestLockDetails> Lock;
        public readonly bool? EnableLockFile;

        [JsonConstructor]
        private ManifestJson(IDictionary<string, string>? dependencies, string? registry,
                             IDictionary<string, ManifestLockDetails>? @lock, bool? enableLockFile)
        {
            Dependencies = dependencies ?? EmptyDictionary<string, string>.InstanceDictionary;
            Registry = registry;
            Lock = @lock ?? EmptyDictionary<string, ManifestLockDetails>.InstanceDictionary;
            EnableLockFile = enableLockFile;
        }

        public static ManifestJson? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ManifestJson>(json);
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    internal class ManifestLockDetails
    {
        public readonly string? Hash;
        public readonly string? Revision;

        [JsonConstructor]
        private ManifestLockDetails(string? hash, string? revision)
        {
            Hash = hash;
            Revision = revision;
        }
    }

    internal class EditorManifestJson
    {
        public readonly IDictionary<string, string>? Recommended;
        public readonly IDictionary<string, string>? DefaultDependencies;
        public readonly IDictionary<string, EditorPackageDetails> Packages;

        [JsonConstructor]
        private EditorManifestJson(IDictionary<string, string>? recommended,
                                   IDictionary<string, string>? defaultDependencies,
                                   IDictionary<string, EditorPackageDetails>? packages)
        {
            Recommended = recommended;
            DefaultDependencies = defaultDependencies;
            Packages = packages ?? EmptyDictionary<string, EditorPackageDetails>.InstanceDictionary;
        }

        public static EditorManifestJson? FromJson(string json)
        {
            return JsonConvert.DeserializeObject<EditorManifestJson>(json);
        }

        public static EditorManifestJson CreateEmpty()
        {
            return new EditorManifestJson(null, null, null);
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    internal class EditorPackageDetails
    {
        public readonly string? Introduced;
        public readonly string? MinimumVersion;
        public readonly string Version;

        [JsonConstructor]
        public EditorPackageDetails(string? introduced, string? minimumVersion,
                                    string? version)
        {
            Introduced = introduced;
            MinimumVersion = minimumVersion;
            Version = version ?? string.Empty;
        }
    }
}
