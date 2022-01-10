using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Extension;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages
{
#region Notes
    // Empirically, this appears to be the process that Unity goes through when adding a new package:
    // 1. Download package
    // 2. Extract to tmp folder in global cache dir, e.g. ~/Library/Unity/cache/packages/packages.unity.com
    // 3. Rename tmp folder to id@version
    // 4. Copy extracted package to tmp folder in project's Library/PackageCache
    // 5. Rename tmp folder to id@version
    // 6. Modify manifest.json (save to copy and replace)
    // 7. Modify packages-lock.json (save to copy and replace) (twice, oddly)
    // 8. Refresh assets database/recompile
    //
    // And when removing:
    // 1. Modify manifest.json (save to copy and replace)
    // 2. Modify packages-lock.json (save to copy and replace) (twice, oddly)
    // 3. Rename Library/PackageCache/id@version to tmp name
    // 4. Delete the tmp folder
    // 5. Refresh assets database/recompile
    //
    // Updating:
    // 1. Download package
    // 2. Extract to tmp folder in global cache dir, e.g. ~/Library/Unity/cache/packages/packages.unity.com
    // 3. Rename tmp folder to id@version
    // 4. Copy extracted package to tmp folder in project's Library/PackageCache
    // 5. Rename tmp folder to id@version
    // 6. Modify manifest.json (save to copy and replace)
    // 7. Modify packages-lock.json (save to copy and replace) (twice, oddly)
    // 8. Rename Library/PackageCache/id@version to tmp name
    // 9. Delete the tmp folder
    // 10. Refresh assets database/recompile
#endregion

    [SolutionComponent]
    public class PackageManager
    {
        private const string DefaultRegistryUrl = "https://packages.unity.com";

        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly ILogger myLogger;
        private readonly UnityVersion myUnityVersion;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly GroupingEvent myDoRefreshGroupingEvent, myWaitForPackagesLockJsonGroupingEvent;
        private readonly DictionaryEvents<string, PackageData> myPackagesById;
        private readonly Dictionary<string, LifetimeDefinition> myPackageLifetimes;
        private readonly VirtualFileSystemPath myPackagesFolder;
        private readonly VirtualFileSystemPath myPackagesLockPath;
        private readonly VirtualFileSystemPath myManifestPath;
        private readonly VirtualFileSystemPath myLocalPackageCacheFolder;

        [CanBeNull] private VirtualFileSystemPath myLastReadGlobalManifestPath;
        [CanBeNull] private EditorManifestJson myGlobalManifest;

        public PackageManager(Lifetime lifetime, ISolution solution, ILogger logger,
                              UnitySolutionTracker unitySolutionTracker,
                              UnityVersion unityVersion,
                              IFileSystemTracker fileSystemTracker)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLogger = logger;
            myUnityVersion = unityVersion;
            myFileSystemTracker = fileSystemTracker;

            // Refresh the packages in the guarded context, safe from reentrancy
            myDoRefreshGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "Unity::PackageManager",
                TimeSpan.FromMilliseconds(500), Rgc.Guarded, DoRefresh);
            myWaitForPackagesLockJsonGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime,
                "Unity::PackageManager::WaitForPackagesLockJson", TimeSpan.FromMilliseconds(2000), Rgc.Guarded,
                DoRefresh);

            myPackagesById = new DictionaryEvents<string, PackageData>(lifetime, "Unity::PackageManager");
            myPackageLifetimes = new Dictionary<string, LifetimeDefinition>();

            myPackagesFolder = mySolution.SolutionDirectory.Combine("Packages");
            myPackagesLockPath = myPackagesFolder.Combine("packages-lock.json");
            myManifestPath = myPackagesFolder.Combine("manifest.json");
            myLocalPackageCacheFolder = mySolution.SolutionDirectory.Combine("Library/PackageCache");

            Updating = new Property<bool?>(lifetime, "PackageManger::Update");

            unitySolutionTracker.IsUnityProject.AdviseUntil(lifetime, value =>
            {
                if (!value) return false;

                ScheduleRefresh();

                // Track changes to the Packages folder, non-recursively. This will handle manifest.json,
                // packages-lock.json and any folders that are added/deleted/renamed
                fileSystemTracker.AdviseDirectoryChanges(lifetime, myPackagesFolder, false, OnPackagesFolderUpdate);

                // We're all set up, terminate the advise
                return true;
            });
        }

        public Property<bool?> Updating { get; }

        public ViewableProperty<bool> IsInitialUpdateFinished { get; } = new ViewableProperty<bool>(false);

        // DictionaryEvents uses locks internally, so this is thread safe. It gets updated from the guarded reentrancy
        // context, so all callbacks also happen within the guarded reentrancy context
        public IReadonlyCollectionEvents<KeyValuePair<string, PackageData>> Packages => myPackagesById;

        [CanBeNull]
        public PackageData GetPackageById(string id)
        {
            return myPackagesById.TryGetValue(id, out var packageData) ? packageData : null;
        }

        [CanBeNull]
        public PackageData GetOwningPackage(VirtualFileSystemPath path)
        {
            foreach (var packageData in myPackagesById.Values)
            {
                if (packageData.PackageFolder != null && packageData.PackageFolder.IsPrefixOf(path))
                    return packageData;
            }

            return null;
        }

        public void RefreshPackages() => ScheduleRefresh();

        private void ScheduleRefresh()
        {
            myLogger.Trace("Scheduling package refresh");
            myDoRefreshGroupingEvent.FireIncoming();
        }

        private void OnPackagesFolderUpdate(FileSystemChangeDelta change)
        {
            var manifestChange = change.FindChangeDelta(myManifestPath) != null;
            var lockChange = change.FindChangeDelta(myPackagesLockPath) != null;

            if (manifestChange && !lockChange)
            {
                // We prefer to use packages-lock.json, as it's more accurate than manifest.json. So if the manifest is
                // changed without also changing the lock file, fire the delayed refresh event, to give Unity a chance
                // to resolve the change changed packages and update the lock file.
                // If we get a subsequent notification for the lock file, we'll cancel the delayed refresh and fire the
                // normal refresh, and use the more accurate lock file to populate our packages list.
                // If we don't get a notification (perhaps Unity isn't running, or the user hasn't yet switched back to
                // Unity) the delayed refresh will still update, using the less accurate method based on manifest.json
                if (myPackagesLockPath.ExistsFile && myManifestPath.ExistsFile &&
                    myManifestPath.FileModificationTimeUtc > myPackagesLockPath.FileModificationTimeUtc)
                {
                    myLogger.Trace("manifest.json modified and is newer than packages-lock.json. Scheduling delayed refresh");
                    myWaitForPackagesLockJsonGroupingEvent.FireIncoming();
                }
                else
                {
                    myLogger.Trace("manifest.json modified. No need to wait for packages-lock.json. Scheduling normal refresh");
                    myDoRefreshGroupingEvent.FireIncoming();
                }
            }
            else if (lockChange)
            {
                myLogger.Trace("packages-lock.json modified. Cancelling delayed refresh. Scheduling normal refresh");
                myWaitForPackagesLockJsonGroupingEvent.CancelIncoming();
                myDoRefreshGroupingEvent.FireIncoming();
            }
            else
            {
                myLogger.Trace("Other file modification in Packages folder. Scheduling normal refresh");
                myDoRefreshGroupingEvent.FireIncoming();
            }
        }

        [Guard(Rgc.Guarded)]
        private void DoRefresh()
        {
            myLogger.Trace("DoRefresh");

            // We only get null if something has gone wrong, such as invalid or missing files (already logged). If we
            // read the files successfully, we'd at least have an empty list. If something is wrong, don't wipe out the
            // current list of packages. It's better to show outdated information than nothing at all
            var newPackages = GetPackages();
            if (newPackages != null)
                myLogger.DoActivity("UpdatePackages", null, () => UpdatePackages(newPackages));
        }

        private void UpdatePackages(IReadOnlyCollection<PackageData> newPackages)
        {
            if (newPackages.Count == 0)
                return;

            Updating.Value = true;
            try
            {
                var existingPackages = myPackagesById.Keys.ToJetHashSet();
                foreach (var packageData in newPackages)
                {
                    // If the package.json file has been updated, remove the entry and add the new one. This should
                    // capture all changes to data + metadata. We don't care too much about duplicates, as this is
                    // invalid JSON and Unity complains. Last write wins, but at least we won't crash
                    if (myPackagesById.TryGetValue(packageData.Id, out var existingPackageData)
                        && existingPackageData.PackageJsonTimestamp != packageData.PackageJsonTimestamp)
                    {
                        RemovePackage(packageData.Id);
                    }

                    if (!myPackagesById.ContainsKey(packageData.Id))
                    {
                        var lifetimeDefinition = myLifetime.CreateNested();
                        myPackagesById.Add(lifetimeDefinition.Lifetime, packageData.Id, packageData);

                        // Note that myPackagesLifetimes is only accessed inside this method, so is thread safe
                        myPackageLifetimes.Add(lifetimeDefinition.Lifetime, packageData.Id, lifetimeDefinition);

                        // Refresh if any editable package.json is modified, so we pick up changes to e.g. dependencies,
                        // display name, etc. We don't care if BuiltIn or Registry packages are modified because they're
                        // not user editable
                        if (packageData.Source == PackageSource.Local && packageData.PackageFolder != null)
                        {
                            myFileSystemTracker.AdviseFileChanges(lifetimeDefinition.Lifetime,
                                packageData.PackageFolder.Combine("package.json"),
                                _ => ScheduleRefresh());
                        }
                    }

                    existingPackages.Remove(packageData.Id);
                }

                // Remove any left overs
                foreach (var id in existingPackages)
                    RemovePackage(id);

            }
            finally
            {
                Updating.Value = false;
                IsInitialUpdateFinished.Value = true;
            }
        }

        private void RemovePackage(string packageId)
        {
            if (myPackageLifetimes.TryGetValue(packageId, out var lifetimeDefinition))
                lifetimeDefinition.Terminate();
        }

        [CanBeNull]
        private List<PackageData> GetPackages()
        {
            return myLogger.Verbose().DoCalculation("GetPackages", null,
                () => GetPackagesFromPackagesLockJson() ?? GetPackagesFromManifestJson(),
                p => p != null ? $"{p.Count} packages" : "Null list of packages. Something went wrong");
        }

        // Introduced officially in 2019.4, but available behind a switch in manifest.json in 2019.3
        // https://forum.unity.com/threads/add-an-option-to-auto-update-packages.730628/#post-4931882
        [CanBeNull]
        private List<PackageData> GetPackagesFromPackagesLockJson()
        {
            if (!myPackagesLockPath.ExistsFile)
            {
                myLogger.Verbose("packages-lock.json does not exist");
                return null;
            }

            if (myManifestPath.ExistsFile &&
                myManifestPath.FileModificationTimeUtc > myPackagesLockPath.FileModificationTimeUtc)
            {
                myLogger.Info("packages-lock.json is out of date. Skipping");
                return null;
            }

            myLogger.Verbose("Getting packages from packages-lock.json");

            var appPath = myUnityVersion.GetActualAppPathForSolution();
            var builtInPackagesFolder = UnityInstallationFinder.GetBuiltInPackagesFolder(appPath);

            return myLogger.CatchSilent(() =>
            {
                var packagesLockJson = PackagesLockJson.FromJson(myPackagesLockPath.ReadAllText2().Text);

                var packages = new List<PackageData>();
                foreach (var (id, details) in packagesLockJson.Dependencies)
                    packages.Add(GetPackageData(id, details, builtInPackagesFolder));

                return packages;
            });
        }

        [CanBeNull]
        private List<PackageData> GetPackagesFromManifestJson()
        {
            if (!myManifestPath.ExistsFile)
            {
                // This is not really expected, unless we're on an older Unity that doesn't support package manager
                myLogger.Info("manifest.json does not exist");
                return null;
            }

            myLogger.Verbose("Getting packages from manifest.json");

            try
            {
                var projectManifest = ManifestJson.FromJson(myManifestPath.ReadAllText2().Text);

                // Now we've deserialised manifest.json, log why we skipped packages-lock.json
                LogWhySkippedPackagesLock(projectManifest);

                var appPath = myUnityVersion.GetActualAppPathForSolution();
                var builtInPackagesFolder = UnityInstallationFinder.GetBuiltInPackagesFolder(appPath);

                // Read the editor's default manifest, which gives us the minimum versions for various packages
                var globalManifestPath = UnityInstallationFinder.GetPackageManagerDefaultManifest(appPath);
                if (globalManifestPath.ExistsFile && myLastReadGlobalManifestPath != globalManifestPath)
                {
                    myLastReadGlobalManifestPath = globalManifestPath;
                    myGlobalManifest = SafelyReadGlobalManifestFile(globalManifestPath);
                }

                // TODO: Support registry scopes
                // Not massively important. We need the registry for a pre-2018.3 cache folder, which I think predates
                // scopes. Post 2018.3, we should get the package from the project local cache
                var registry = projectManifest.Registry ?? DefaultRegistryUrl;

                var packages = new Dictionary<string, PackageData>();
                foreach (var (id, version) in projectManifest.Dependencies)
                {
                    if (version.Equals("exclude", StringComparison.OrdinalIgnoreCase))
                        continue;

                    projectManifest.Lock.TryGetValue(id, out var lockDetails);
                    packages[id] = GetPackageData(id, version, registry, builtInPackagesFolder,
                        lockDetails);
                }

                // If a child folder of Packages has a package.json file, then it's a package
                foreach (var child in myPackagesFolder.GetChildDirectories())
                {
                    // The folder name is not reliable to act as ID, so we'll use the ID from package.json. All other
                    // packages get the ID from manifest.json or packages-lock.json. This is assumed to be the same as
                    // the ID in package.json
                    var packageData = GetPackageDataFromFolder(null, child, PackageSource.Embedded);
                    if (packageData != null)
                        packages[packageData.Id] = packageData;
                }

                // We currently have the project dependencies. These will usually be the version requested, and will
                // therefore have package data, as long as that data exists in the cache. However, a transitive
                // dependency might get resolved to a higher version, so the project dependency version won't be in the
                // cache, and that package data will be missing.

                // Let's calculate the transitive dependencies.
                // This is a very naive implementation, initially based on observation. UPM will try to resolve
                // dependencies based on a resolution strategy. Note that this is not a conflict resolution strategy. It
                // applies to all dependencies, even if there is only usage of that package.
                // The default resolution strategy is "lowest". For a single package, this means get that version. For
                // multiple packages it means get the lowest version that meets all version requirements, which
                // translates to the highest common version.
                // With one of the "highest*" resolution strategies, UPM will choose the highest patch, minor or major
                // version that's available on the server. E.g. if two packages require dependency A@1.0.0 and A@1.0.5,
                // then UPM can resolve this to A@1.0.7 or A@1.1.0 or A@20.0.0. This causes us problems because we don't
                // have that information (although it is cached elsewhere on disk). If this dependency is used as a
                // project dependency, then it also updates the project dependency.
                // We fake "highest*" resolution by getting whatever version is available in Library/PackagesCache.
                var packagesToProcess = new List<PackageData>(packages.Values);
                while (packagesToProcess.Count > 0)
                {
                    var foundDependencies = GetPackagesFromDependencies(registry, packages, packagesToProcess);
                    foreach (var package in foundDependencies)
                        packages[package.Id] = package;

                    packagesToProcess = foundDependencies;
                }

                // TODO: Strip unused packages
                // There is a chance we have introduced an extra package via a dependency that is subsequently updated.
                // E.g. a dependency introduces A@1.0.0 which introduces B@1.0.0. If we have another package that
                // depends on A@2.0.0 which no longer uses B, then we have an orphaned package
                // This is an unlikely edge case, as it means we'd have to resolve the old version correctly as well as
                // the new one. And the worst that can happen is we show an extra package in the UI

                return new List<PackageData>(packages.Values);
            }
            catch (Exception e)
            {
                myLogger.LogExceptionSilently(e);
                return null;
            }
        }

        private void LogWhySkippedPackagesLock(ManifestJson projectManifest)
        {
            // We know myManifestPath exists
            if (myPackagesLockPath.ExistsFile &&
                myManifestPath.FileModificationTimeUtc > myPackagesLockPath.FileModificationTimeUtc)
            {
                if (projectManifest.EnableLockFile.HasValue && !projectManifest.EnableLockFile.Value)
                {
                    myLogger.Info("packages-lock.json is out of date. Lock file is disabled in manifest.json. Old file needs deleting");
                }
                else if (myUnityVersion.ActualVersionForSolution.Value < new Version(2019, 3))
                {
                    myLogger.Info(
                        "packages-lock.json is not supported by this version of Unity. Perhaps the file is from a newer version?");
                }

                myLogger.Info("packages-lock.json out of date. Most likely reason: Unity not running");
            }
        }

        [NotNull]
        private EditorManifestJson SafelyReadGlobalManifestFile(VirtualFileSystemPath globalManifestPath)
        {
            try
            {
                return EditorManifestJson.FromJson(globalManifestPath.ReadAllText2().Text);
            }
            catch (Exception e)
            {
                // Even if there's an error, cache an empty file, so we don't continually try to read a broken file
                myLogger.LogExceptionSilently(e);
                return EditorManifestJson.CreateEmpty();
            }
        }

        [NotNull]
        private PackageData GetPackageData(string id, PackagesLockDependency details,
                                           VirtualFileSystemPath builtInPackagesFolder)
        {
            try
            {
                PackageData packageData = null;
                switch (details.Source)
                {
                    case "embedded":
                        packageData = GetEmbeddedPackage(id, details.Version);
                        break;
                    case "registry":
                        packageData = GetRegistryPackage(id, details.Version, details.Url ?? DefaultRegistryUrl);
                        break;
                    case "builtin":
                        packageData = GetBuiltInPackage(id, details.Version, builtInPackagesFolder);
                        break;
                    case "git":
                        packageData = GetGitPackage(id, details.Version, details.Hash);
                        break;
                    case "local":
                        packageData = GetLocalPackage(id, details.Version);
                        break;
                    case "local-tarball":
                        packageData = GetLocalTarballPackage(id, details.Version);
                        break;
                }

                return packageData ?? PackageData.CreateUnknown(id, details.Version);
            }
            catch (Exception e)
            {
                myLogger.Error(e, $"Error resolving package {id}");
                return PackageData.CreateUnknown(id, details.Version);
            }
        }

        private PackageData GetPackageData(string id, string version, string registry,
                                           VirtualFileSystemPath builtInPackagesFolder,
                                           [CanBeNull] ManifestLockDetails lockDetails)
        {
            // Order is important here. A package can be listed in manifest.json, but if it also exists in Packages,
            // that embedded copy takes precedence. We look for an embedded folder with the same name, but it can be
            // under any name - we'll find it again and override it when we look at the other embedded packages.
            // Registry packages are the most likely to match, and can't clash with other packages, so check them early.
            // The file: protocol is used by local but it can also be a protocol for git, so check git before local.
            try
            {
                return GetEmbeddedPackage(id, id)
                       ?? GetRegistryPackage(id, version, registry)
                       ?? GetGitPackage(id, version, lockDetails?.Hash, lockDetails?.Revision)
                       ?? GetLocalPackage(id, version)
                       ?? GetLocalTarballPackage(id, version)
                       ?? GetBuiltInPackage(id, version, builtInPackagesFolder)
                       ?? PackageData.CreateUnknown(id, version);
            }
            catch (Exception e)
            {
                myLogger.Error(e, $"Error resolving package {id}");
                return PackageData.CreateUnknown(id, version);
            }
        }

        [CanBeNull]
        private PackageData GetEmbeddedPackage(string id, string filePath)
        {
            // Embedded packages live in the Packages folder. When reading from manifest.json, filePath is the same as
            // ID. When reading from packages-lock.json, we already know it's an embedded folder, and use the version,
            // which has a 'file:' prefix
            var packageFolder = myPackagesFolder.Combine(filePath.TrimFromStart("file:"));
            return GetPackageDataFromFolder(id, packageFolder, PackageSource.Embedded);
        }

        [CanBeNull]
        private PackageData GetRegistryPackage(string id, string version, string registryUrl)
        {
            // When parsing manifest.json, version might be a version, or it might even be a URL for a git package
            var cacheFolder = RelativePath.TryParse($"{id}@{version}");
            if (cacheFolder.IsEmpty)
                return null;

            // Unity 2018.3 introduced an additional layer of caching for registry based packages, local to the
            // project, so that any edits to the files in the package only affect this project. This is primarily
            // for the API updater, which would otherwise modify files in the product wide cache
            var packageData = GetPackageDataFromFolder(id, myLocalPackageCacheFolder.Combine(cacheFolder),
                PackageSource.Registry);
            if (packageData != null)
                return packageData;

            // Fall back to the product wide cache
            var packageCacheFolder = UnityCachesFinder.GetPackagesCacheFolder(registryUrl);
            if (packageCacheFolder == null || !packageCacheFolder.ExistsDirectory)
                return null;

            var packageFolder = packageCacheFolder.Combine(cacheFolder);
            return GetPackageDataFromFolder(id, packageFolder, PackageSource.Registry);
        }

        [CanBeNull]
        private PackageData GetBuiltInPackage(string id, string version, VirtualFileSystemPath builtInPackagesFolder)
        {
            // Starting with Unity 2020.3, modules are copied into the local package cache, and compiled from there.
            // This behaviour has been backported to 2019.4 LTS. Make sure we use the cached files, or breakpoints won't
            // resolve, because we'll be showing different files to what's in the PDB.
            // I don't know why the files are copied - it makes sense for registry packages to be copied so that script
            // updater can run on them, or users can make (dangerously transient) changes. But built in packages are,
            // well, built in, and should be up to date as far as the script updater is concerned.
            var localCacheFolder = myLocalPackageCacheFolder.Combine($"{id}@{version}");
            var packageData = GetPackageDataFromFolder(id, localCacheFolder, PackageSource.BuiltIn);
            if (packageData == null && builtInPackagesFolder.IsNotEmpty)
                packageData = GetPackageDataFromFolder(id, builtInPackagesFolder.Combine(id), PackageSource.BuiltIn);
            if (packageData != null)
                return packageData;

            // We can't find the actual package. If we "know" it's a module/built in package, then mark it as an
            // unresolved built in package, rather than just an unresolved package. The Unity Explorer can use this to
            // put the unresolved package in the right place, rather than show as a top level unresolved package simply
            // because we haven't found the application package cache yet.
            // We can rely on an ID starting with "com.unity.modules." as this is the convention Unity uses. Since they
            // control the namespace of their own registries, we can be confident that they won't publish normal
            // packages with the same naming convention. We can't be sure for third part registries, but if anyone does
            // that, they only have themselves to blame.
            // If we don't recognise the package as a built in, let someone else handle it
            return id.StartsWith("com.unity.modules.")
                ? PackageData.CreateUnknown(id, version, PackageSource.BuiltIn)
                : null;
        }

        [CanBeNull]
        private PackageData GetGitPackage(string id, string version, [CanBeNull] string hash,
                                          [CanBeNull] string revision = null)
        {
            // For older Unity versions, manifest.json will have a hash for any git based package. For newer Unity
            // versions, this is stored in packages-lock.json. If the lock file is disabled, then we don't get a hash
            // and have to figure it out based on whatever is in Library/PackagesCache. We check the vesion as a git
            // URL based on the docs: https://docs.unity3d.com/Manual/upm-git.html
            if (hash == null && !IsGitUrl(version))
                return null;

            // This must be a git package, make sure we return something
            try
            {
                var packageFolder = myLocalPackageCacheFolder.Combine($"{id}@{hash}");
                if (!packageFolder.ExistsDirectory && hash != null)
                {
                    var shortHash = hash.Substring(0, Math.Min(hash.Length, 10));
                    packageFolder = myLocalPackageCacheFolder.Combine($"{id}@{shortHash}");
                }

                if (!packageFolder.ExistsDirectory)
                    packageFolder = myLocalPackageCacheFolder.GetChildDirectories($"{id}@*").FirstOrDefault();

                if (packageFolder != null && packageFolder.ExistsDirectory)
                {
                    return GetPackageDataFromFolder(id, packageFolder, PackageSource.Git,
                        new GitDetails(version, hash, revision));
                }

                return null;
            }
            catch (Exception e)
            {
                myLogger.Error(e, "Error resolving git package");
                return PackageData.CreateUnknown(id, version);
            }
        }

        private static bool IsGitUrl(string version)
        {
            return Uri.TryCreate(version, UriKind.Absolute, out var url) &&
                   (url.Scheme.StartsWith("git+") ||
                    url.AbsolutePath.EndsWith(".git", StringComparison.InvariantCultureIgnoreCase));
        }

        [CanBeNull]
        private PackageData GetLocalPackage(string id, string version)
        {
            // If the version doesn't start with "file:" we know it's not a local package
            if (!version.StartsWith("file:"))
                return null;

            // This might be a local package, or it might be a local tarball. Or a git package (although we'll have
            // resolved that before trying local packages), so return null if we can't resolve it
            try
            {
                var path = version.Substring(5);
                var packageFolder = myPackagesFolder.Combine(path);
                return packageFolder.ExistsDirectory
                    ? GetPackageDataFromFolder(id, packageFolder, PackageSource.Local)
                    : null;
            }
            catch (Exception e)
            {
                myLogger.Error(e, $"Error resolving local package {id} at {version}");
                return null;
            }
        }

        [CanBeNull]
        private PackageData GetLocalTarballPackage(string id, string version)
        {
            if (!version.StartsWith("file:"))
                return null;

            // This is a package installed from a package.tgz file. The original file is referenced, but not touched.
            // It is expanded into Library/PackageCache with a filename of name@{md5-of-path}-{file-modification-in-epoch-ms}
            try
            {
                var path = version.Substring(5);
                var tarballPath = myPackagesFolder.Combine(path);
                if (tarballPath.ExistsFile)
                {
                    // Note that this is inherently fragile. If the file is touched, but not imported (i.e. Unity isn't
                    // running), we won't be able to resolve it at all. We also don't have a watch on the file, so we
                    // have no way of knowing that the package has been updated or imported.
                    // On the plus side, I have yet to see anyone talk about tarball packages in the wild.
                    // Also, once it's been imported, we'll refresh and all will be good
                    var timestamp = (long) (tarballPath.FileModificationTimeUtc - DateTimeEx.UnixEpoch).TotalMilliseconds;
                    var hash = GetMd5OfString(tarballPath.FullPath).Substring(0, 12).ToLowerInvariant();

                    var packageFolder = myLocalPackageCacheFolder.Combine($"{id}@{hash}-{timestamp}");
                    var tarballLocation = tarballPath.StartsWith(mySolution.SolutionDirectory)
                        ? tarballPath.RemovePrefix(mySolution.SolutionDirectory.Parent)
                        : tarballPath;
                    return GetPackageDataFromFolder(id, packageFolder, PackageSource.LocalTarball,
                        tarballLocation: tarballLocation);
                }

                return null;
            }
            catch (Exception e)
            {
                myLogger.Error(e, $"Error resolving local tarball package {version}");
                return null;
            }
        }

        [CanBeNull]
        private PackageData GetPackageDataFromFolder([CanBeNull] string id,
                                                     [NotNull] VirtualFileSystemPath packageFolder,
                                                     PackageSource packageSource,
                                                     [CanBeNull] GitDetails gitDetails = null,
                                                     [CanBeNull] VirtualFileSystemPath tarballLocation = null)
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
                        myLogger.LogExceptionSilently(e);
                        return null;
                    }
                }
            }

            return null;
        }

        private static string GetMd5OfString(string value)
        {
            // Use input string to calculate MD5 hash
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(value);
                var hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                var sb = new StringBuilder();
                foreach (var t in hashBytes)
                    sb.Append(t.ToString("X2"));

                return sb.ToString().PadLeft(32, '0');
            }
        }

        private List<PackageData> GetPackagesFromDependencies([NotNull] string registry,
                                                              Dictionary<string, PackageData> resolvedPackages,
                                                              List<PackageData> packagesToProcess)
        {
            var dependencies = new Dictionary<string, JetSemanticVersion>();

            // Find the highest requested version of each dependency of each package being processed. Check all
            // dependencies, even if we've already resolved it, in case we find a higher version
            foreach (var packageData in packagesToProcess)
            {
                foreach (var (id, versionString) in packageData.PackageDetails.Dependencies)
                {
                    if (DoesResolvedPackageTakePrecedence(id, resolvedPackages))
                        continue;

                    if (!JetSemanticVersion.TryParse(versionString, out var dependencyVersion))
                        continue;

                    var currentMaxVersion = GetCurrentMaxVersion(id, dependencies);
                    var minimumVersion = GetMinimumVersion(id);

                    dependencies[id] = Max(dependencyVersion, Max(currentMaxVersion, minimumVersion));
                }
            }

            ICollection<VirtualFileSystemPath> cachedPackages = null;
            var newPackages = new List<PackageData>();
            foreach (var (id, version) in dependencies)
            {
                if (version > GetResolvedVersion(id, resolvedPackages))
                {
                    if (cachedPackages == null) cachedPackages = myLocalPackageCacheFolder.GetChildDirectories();

                    // We know this is a registry package, so try to get it from the local cache. It might be missing:
                    // 1) the cache hasn't been built yet
                    // 2) the package has been resolved with one of the "highest*" strategies and a newer version has
                    //    been downloaded from the UPM server. Check for any "id@" folders, and use that as the version.
                    //    If it's in the local cache, it's the (last) resolved version. If Unity isn't running and the
                    //    manifest is out of date, we can only do a best effort attempt at showing the right packages.
                    //    We need Unity to resolve. We'll refresh once Unity has started again.
                    // So:
                    // 1) Check for the exact version in the local cache
                    // 2) Check for any version in the local cache
                    // 3) Check for the exact version in the global cache
                    PackageData packageData = null;
                    var exact = $"{id}@{version}";
                    var prefix = $"{id}@";
                    foreach (var packageFolder in cachedPackages)
                    {
                        if (packageFolder.Name.Equals(exact, StringComparison.InvariantCultureIgnoreCase)
                            || packageFolder.Name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            packageData = GetPackageDataFromFolder(id, packageFolder, PackageSource.Registry);
                            if (packageData != null)
                                break;
                        }
                    }

                    if (packageData == null)
                    {
                        var packageFolder = UnityCachesFinder.GetPackagesCacheFolder(registry)?.Combine(exact);
                        if (packageFolder != null)
                            packageData = GetPackageDataFromFolder(id, packageFolder, PackageSource.Registry);
                    }

                    if (packageData != null)
                        newPackages.Add(packageData);
                }
            }

            return newPackages;
        }

        private static bool DoesResolvedPackageTakePrecedence(string id,
                                                              IReadOnlyDictionary<string, PackageData> resolvedPackages)
        {
            // Some package types take precedence over any requests for another version. Basically any package that is
            // built in or pointing at actual files
            return resolvedPackages.TryGetValue(id, out var packageData) &&
                   (packageData.Source == PackageSource.Embedded
                   || packageData.Source == PackageSource.BuiltIn
                   || packageData.Source == PackageSource.Git
                   || packageData.Source == PackageSource.Local
                   || packageData.Source == PackageSource.LocalTarball);
        }

        private static JetSemanticVersion GetCurrentMaxVersion(
            string id, IReadOnlyDictionary<string, JetSemanticVersion> dependencies)
        {
            return dependencies.TryGetValue(id, out var version) ? version : JetSemanticVersion.Empty;
        }

        private JetSemanticVersion GetMinimumVersion(string id)
        {
            EditorPackageDetails editorPackageDetails = null;
            if (myGlobalManifest?.Packages.TryGetValue(id, out editorPackageDetails) == true
                && JetSemanticVersion.TryParse(editorPackageDetails?.MinimumVersion, out var version))
            {
                return version;
            }

            return JetSemanticVersion.Empty;
        }

        private static JetSemanticVersion GetResolvedVersion(string id, Dictionary<string, PackageData> resolvedPackages)
        {
            if (resolvedPackages.TryGetValue(id, out var packageData) &&
                JetSemanticVersion.TryParse(packageData.PackageDetails.Version, out var version))
            {
                return version;
            }

            return JetSemanticVersion.Empty;
        }

        private static JetSemanticVersion Max(JetSemanticVersion v1, JetSemanticVersion v2)
        {
            return v1 > v2 ? v1 : v2;
        }
    }
}
