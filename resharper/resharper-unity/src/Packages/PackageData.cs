using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Util;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace JetBrains.ReSharper.Plugins.Unity.Packages
{
    public class PackageData
    {
        [NotNull] public readonly string Id;
        [CanBeNull] public readonly VirtualFileSystemPath PackageFolder;
        public readonly DateTime PackageJsonTimestamp;
        public readonly PackageDetails PackageDetails;
        public readonly PackageSource Source;
        [CanBeNull] public readonly GitDetails GitDetails;
        [CanBeNull] public readonly VirtualFileSystemPath TarballLocation;

        public PackageData([NotNull] string id,
                           [CanBeNull] VirtualFileSystemPath packageFolder,
                           DateTime packageJsonTimestamp,
                           PackageDetails packageDetails,
                           PackageSource source,
                           [CanBeNull] GitDetails gitDetails,
                           [CanBeNull] VirtualFileSystemPath tarballLocation)
        {
            Id = id;
            PackageFolder = packageFolder;
            PackageJsonTimestamp = packageJsonTimestamp;
            PackageDetails = packageDetails;
            Source = source;
            GitDetails = gitDetails;
            TarballLocation = tarballLocation;
        }

        // Even if project generation is enabled, not all pacakges are intended to be user editable
        public bool IsUserEditable => Source is PackageSource.Embedded or PackageSource.Local;

        public static PackageData CreateUnknown(string id, string version,
                                                PackageSource packageSource = PackageSource.Unknown)
        {
            return new PackageData(id, null, DateTime.MinValue,
                new PackageDetails(id, $"{id}@{version}", version,
                    $"Cannot resolve package '{id}' with version '{version}'",
                    new Dictionary<string, string>()), packageSource, null, null);
        }
    }

    public class PackageDetails
    {
        // Note that canonical name is the name field from package.json. It is the truth about the name of the package.
        // The id field in PackageData is the ID used to reference the package in manifest.json or packages-lock.json.
        // The assumption is that these values are always the same.
        [NotNull] public readonly string CanonicalName;
        [NotNull] public readonly string DisplayName;
        [NotNull] public readonly string Version;
        [CanBeNull] public readonly string Description;
        // [CanBeNull] public readonly string Author;  // Author might actually be a dictionary
        [NotNull] public readonly Dictionary<string, string> Dependencies;

        public PackageDetails([NotNull] string canonicalName,
                              [NotNull] string displayName,
                              [NotNull] string version,
                              [CanBeNull] string description,
                              [NotNull] Dictionary<string, string> dependencies)
        {
            CanonicalName = canonicalName;
            DisplayName = displayName;
            Version = version;
            Description = description;
            Dependencies = dependencies;
        }

        [NotNull]
        internal static PackageDetails FromPackageJson([NotNull] PackageJson packageJson, VirtualFileSystemPath packageFolder)
        {
            var name = packageJson.Name ?? packageFolder.Name;
            return new PackageDetails(name, packageJson.DisplayName ?? name, packageJson.Version ?? string.Empty,
                packageJson.Description, packageJson.Dependencies);
        }
    }

    public class GitDetails
    {
        [NotNull] public readonly string Url;
        [CanBeNull] public readonly string Hash;
        [CanBeNull] public readonly string Revision;

        public GitDetails([NotNull] string url, [CanBeNull] string hash, [CanBeNull] string revision)
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

        public static PackagesLockJson FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PackagesLockJson>(json);
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    internal class PackagesLockDependency
    {
        public readonly string Version;
        public readonly int? Depth;
        [CanBeNull] public readonly string Source;
        [NotNull] public readonly Dictionary<string, string> Dependencies;
        [CanBeNull] public readonly string Url;
        [CanBeNull] public readonly string Hash;

        public PackagesLockDependency(string version, int? depth, [CanBeNull] string source,
                                      [NotNull] Dictionary<string, string> dependencies, [CanBeNull] string url,
                                      [CanBeNull] string hash)
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
        [CanBeNull] public readonly string Name;
        [CanBeNull] public readonly string DisplayName;
        [CanBeNull] public readonly string Version;
        [CanBeNull] public readonly string Description;
        // [CanBeNull] public readonly string Author; // TODO: Author might be a map<string, string>, e.g. author[name]
        [NotNull] public readonly Dictionary<string, string> Dependencies;

        [JsonConstructor]
        private PackageJson([CanBeNull] string name,
                            [CanBeNull] string displayName,
                            [CanBeNull] string version,
                            [CanBeNull] string description,
                            [CanBeNull] Dictionary<string, string> dependencies)
        {
            Name = name;
            DisplayName = displayName;
            Version = version;
            Description = description;
            Dependencies = dependencies ?? new Dictionary<string, string>();
        }

        public static PackageJson FromJson(string json)
        {
            return JsonConvert.DeserializeObject<PackageJson>(json);
        }
    }

    internal class ManifestJson
    {
        [NotNull] public readonly IDictionary<string, string> Dependencies;
        [CanBeNull] public readonly string Registry;
        [NotNull] public readonly IDictionary<string, ManifestLockDetails> Lock;
        public readonly bool? EnableLockFile;

        [JsonConstructor]
        private ManifestJson([CanBeNull] IDictionary<string, string> dependencies, [CanBeNull] string registry,
                             [CanBeNull] IDictionary<string, ManifestLockDetails> @lock, bool? enableLockFile)
        {
            Dependencies = dependencies ?? EmptyDictionary<string, string>.InstanceDictionary;
            Registry = registry;
            Lock = @lock ?? EmptyDictionary<string, ManifestLockDetails>.InstanceDictionary;
            EnableLockFile = enableLockFile;
        }

        public static ManifestJson FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ManifestJson>(json);
        }
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    internal class ManifestLockDetails
    {
        [CanBeNull] public readonly string Hash;
        [CanBeNull] public readonly string Revision;

        [JsonConstructor]
        private ManifestLockDetails([CanBeNull] string hash, [CanBeNull] string revision)
        {
            Hash = hash;
            Revision = revision;
        }
    }

    internal class EditorManifestJson
    {
        [CanBeNull] public readonly IDictionary<string, string> Recommended;
        [CanBeNull] public readonly IDictionary<string, string> DefaultDependencies;
        [NotNull] public readonly IDictionary<string, EditorPackageDetails> Packages;

        [JsonConstructor]
        private EditorManifestJson([CanBeNull] IDictionary<string, string> recommended,
                                   [CanBeNull] IDictionary<string, string> defaultDependencies,
                                   [CanBeNull] IDictionary<string, EditorPackageDetails> packages)
        {
            Recommended = recommended;
            DefaultDependencies = defaultDependencies;
            Packages = packages ?? EmptyDictionary<string, EditorPackageDetails>.InstanceDictionary;
        }

        public static EditorManifestJson FromJson(string json)
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
        [CanBeNull] public readonly string Introduced;
        [CanBeNull] public readonly string MinimumVersion;
        [NotNull] public readonly string Version;

        [JsonConstructor]
        public EditorPackageDetails([CanBeNull] string introduced, [CanBeNull] string minimumVersion,
                                    [CanBeNull] string version)
        {
            Introduced = introduced;
            MinimumVersion = minimumVersion;
            Version = version ?? string.Empty;
        }
    }
}
