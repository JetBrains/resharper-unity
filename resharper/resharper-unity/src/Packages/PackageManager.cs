using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Extension;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Packages
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
        private readonly GroupingEvent myGroupingEvent;
        private readonly DictionaryEvents<string, PackageData> myPackagesById;
        private readonly Dictionary<string, LifetimeDefinition> myPackageLifetimes;
        private readonly FileSystemPath myPackagesFolder;
        private readonly FileSystemPath myPackagesLockPath;
        private readonly FileSystemPath myManifestPath;

        [CanBeNull] private FileSystemPath myLastReadGlobalManifestPath;
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

            // Refresh the packages in the guarded context, safe from reentrancy.
            myGroupingEvent = solution.Locks.GroupingEvents.CreateEvent(lifetime, "Unity::PackageManager",
                TimeSpan.FromMilliseconds(500), Rgc.Guarded, DoRefresh);

            myPackagesById = new DictionaryEvents<string, PackageData>(lifetime, "Unity::PackageManager");
            myPackageLifetimes = new Dictionary<string, LifetimeDefinition>();

            myPackagesFolder = mySolution.SolutionDirectory.Combine("Packages");
            myPackagesLockPath = myPackagesFolder.Combine("packages-lock.json");
            myManifestPath = myPackagesFolder.Combine("manifest.json");

            unitySolutionTracker.IsUnityProject.AdviseUntil(lifetime, value =>
            {
                if (!value) return false;

                ScheduleRefresh();

                // Track changes to manifest.json and packages-lock.json. Also track changes in the Packages folder, but
                // only top level, not recursively. We only want to update the packages if a new package has been added
                // or removed
                var packagesFolder = mySolution.SolutionDirectory.Combine("Packages");
                fileSystemTracker.AdviseFileChanges(lifetime, packagesFolder.Combine("packages-lock.json"),
                    _ => ScheduleRefresh());
                fileSystemTracker.AdviseFileChanges(lifetime, packagesFolder.Combine("manifest.json"),
                    _ => ScheduleRefresh());
                fileSystemTracker.AdviseDirectoryChanges(lifetime, packagesFolder, false, _ => ScheduleRefresh());

                // We're all set up, terminate the advise
                return true;
            });
        }

        // DictionaryEvents uses locks internally, so this is thread safe. It gets updated from the guarded reentrancy
        // context, so all callbacks also happen within the guarded reentrancy context
        public IReadonlyCollectionEvents<KeyValuePair<string, PackageData>> Packages => myPackagesById;

        [CanBeNull]
        public PackageData GetPackageById(string id)
        {
            return myPackagesById.TryGetValue(id, out var packageData) ? packageData : null;
        }

        public void RefreshPackages() => ScheduleRefresh();

        private void ScheduleRefresh() => myGroupingEvent.FireIncoming();


        [Guard(Rgc.Guarded)]
        private void DoRefresh()
        {
            // If we're reacting to changes in manifest.json, give Unity a chance to update and refresh packages-lock.json
            if (!AreFilesReadyForReading())
            {
                ScheduleRefresh();
                return;
            }

            var newPackages = GetPackages();
            if (newPackages == null)
            {
                // Something's gone wrong. E.g. invalid JSON files. If things were ok, we'd at least get an empty list.
                // Don't wipe out the current packages, it's better to show outdated info rather than nothing. It should
                // be fixed soon enough, when the game won't start in Unity
                return;
            }

            myLogger.DoActivity("UpdatePackages", null, () => UpdatePackages(newPackages));
        }

        private bool AreFilesReadyForReading()
        {
            if (!myPackagesLockPath.ExistsFile || !myManifestPath.ExistsFile)
                return true;

            return IsPackagesLockUpToDate() || ShouldSkipPackagesLock();
        }

        private bool IsPackagesLockUpToDate()
        {
            return myPackagesLockPath.FileModificationTimeUtc >= myManifestPath.FileModificationTimeUtc;
        }

        private bool ShouldSkipPackagesLock()
        {
            // Has Unity taken too long to update packages-lock.json? If it's taken longer that 2 seconds to update, we
            // fall back to manifest.json. If Unity isn't running, is going slow or doesn't get notified of the change
            // to manifest.json, we'll fall back and still show up to date results. When Unity catches up, we'll get a
            // file change notification on packages-lock.json and update to canonical results.
            // Two seconds is a good default, as Unity appears to resolve packages before reloading the AppDomain

            return myManifestPath.FileModificationTimeUtc - myPackagesLockPath.FileModificationTimeUtc >
                   TimeSpan.FromSeconds(2);
        }

        private void UpdatePackages(IEnumerable<PackageData> newPackages)
        {
            var existingPackages = myPackagesById.Keys.ToJetHashSet();
            foreach (var packageData in newPackages)
            {
                // If the package.json file has been updated, remove the entry and add the new one. This should capture
                // all changes to data + metadata. We don't care too much about duplicates, as this is invalid JSON and
                // Unity complains. Last write wins, but at least we won't crash
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
                return null;

            if (myManifestPath.ExistsFile && ShouldSkipPackagesLock())
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
                return null;

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

                // From observation, Unity treats package folders in the Packages folder as actual packages, even if they're
                // not registered in manifest.json. They must have a */package.json file, in the root of the package itself
                foreach (var child in myPackagesFolder.GetChildDirectories())
                {
                    // The folder name is not reliable to act as ID, so we'll use the ID from package.json. All other
                    // packages get the ID from manifest.json or packages-lock.json. This is assumed to be the same as
                    // the ID in package.json
                    var packageData = GetPackageDataFromFolder(null, child, PackageSource.Embedded);
                    if (packageData != null)
                        packages[packageData.Id] = packageData;
                }

                // Calculate the transitive dependencies. This is all based on observation
                var packagesToProcess = new List<PackageData>(packages.Values);
                while (packagesToProcess.Count > 0)
                {
                    var foundDependencies = GetPackagesFromDependencies(registry, builtInPackagesFolder,
                        packages, packagesToProcess);
                    foreach (var package in foundDependencies)
                        packages[package.Id] = package;

                    packagesToProcess = foundDependencies;
                }

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
            if (ShouldSkipPackagesLock())
            {
                if (projectManifest.EnableLockFile.HasValue && !projectManifest.EnableLockFile.Value)
                {
                    myLogger.Info("packages-lock.json is disabled in manifest.json. Old file needs deleting");
                }
                else if (myUnityVersion.ActualVersionForSolution.Value < new Version(2019, 3))
                {
                    myLogger.Info(
                        "packages-lock.json is not supported by this version of Unity. Perhaps the file is from a newer version?");
                }

                // Most likely reason now is that Unity isn't running
            }
        }

        [NotNull]
        private EditorManifestJson SafelyReadGlobalManifestFile(FileSystemPath globalManifestPath)
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
                                           FileSystemPath builtInPackagesFolder)
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
                                           FileSystemPath builtInPackagesFolder,
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
            // Embedded packages live in the Packages folder. When reading from packages-lock.json, the filePath has a
            // 'file:' prefix. We make sure it's the folder name when there is no packages-lock.json
            var packageFolder = myPackagesFolder.Combine(filePath.TrimFromStart("file:"));
            return GetPackageDataFromFolder(id, packageFolder, PackageSource.Embedded);
        }

        [CanBeNull]
        private PackageData GetRegistryPackage(string id, string version, string registryUrl)
        {
            // The version parameter isn't necessarily a version, and might not parse correctly. When using
            // manifest.json to load packages, we will try to match a registry package before we try to match a git
            // package, so the version might even be a URL
            var cacheFolder = RelativePath.TryParse($"{id}@{version}");
            if (cacheFolder.IsEmpty)
                return null;

            // Unity 2018.3 introduced an additional layer of caching for registry based packages, local to the
            // project, so that any edits to the files in the package only affect this project. This is primarily
            // for the API updater, which would otherwise modify files in the product wide cache
            var packageCacheFolder = mySolution.SolutionDirectory.Combine("Library/PackageCache");
            var packageFolder = packageCacheFolder.Combine(cacheFolder);
            var packageData = GetPackageDataFromFolder(id, packageFolder, PackageSource.Registry);
            if (packageData != null)
                return packageData;

            // Fall back to the product wide cache
            packageCacheFolder = UnityCachesFinder.GetPackagesCacheFolder(registryUrl);
            if (packageCacheFolder == null || !packageCacheFolder.ExistsDirectory)
                return null;

            packageFolder = packageCacheFolder.Combine(cacheFolder);
            return GetPackageDataFromFolder(id, packageFolder, PackageSource.Registry);
        }

        [CanBeNull]
        private PackageData GetBuiltInPackage(string id, string version, FileSystemPath builtInPackagesFolder)
        {
            // If we can identify the module root of the current project, use it to look up the module
            if (builtInPackagesFolder.ExistsDirectory)
            {
                var packageFolder = builtInPackagesFolder.Combine(id);
                return GetPackageDataFromFolder(id, packageFolder, PackageSource.BuiltIn);
            }

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
            // If we don't have a hash, we know this isn't a git package
            if (hash == null)
                return null;

            // This must be a git package, make sure we return something
            try
            {
                var packageFolder = mySolution.SolutionDirectory.Combine($"Library/PackageCache/{id}@{hash}");
                if (!packageFolder.ExistsDirectory)
                {
                    var shortHash = hash.Substring(0, Math.Min(hash.Length, 10));
                    packageFolder = mySolution.SolutionDirectory.Combine($"Library/PackageCache/{id}@{shortHash}");
                }

                return GetPackageDataFromFolder(id, packageFolder, PackageSource.Git,
                    new GitDetails(version, hash, revision));
            }
            catch (Exception e)
            {
                myLogger.Error(e, "Error resolving git package");
                return PackageData.CreateUnknown(id, version);
            }
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

                    var packageFolder = mySolution.SolutionDirectory.Combine($"Library/PackageCache/{id}@{hash}-{timestamp}");
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
                                                     [NotNull] FileSystemPath packageFolder,
                                                     PackageSource packageSource,
                                                     [CanBeNull] GitDetails gitDetails = null,
                                                     [CanBeNull] FileSystemPath tarballLocation = null)
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
                                                              [NotNull] FileSystemPath builtInPackagesFolder,
                                                              Dictionary<string, PackageData> resolvedPackages,
                                                              List<PackageData> packagesToProcess)
        {
            var dependencies = new Dictionary<string, JetSemanticVersion>();

            // Find the highest requested version of each dependency of each package being processed
            foreach (var packageData in packagesToProcess)
            {
                foreach (var (id, version) in packageData.PackageDetails.Dependencies)
                {
                    // If it's been previously resolved, there's nothing more to do. Note that skipping it here means
                    // it's not processed further, including dependencies
                    if (resolvedPackages.ContainsKey(id))
                    {
                        // TODO: If the dependency is a higher version, update it
                        // See https://github.com/JetBrains/resharper-unity/issues/1786
                        continue;
                    }

                    dependencies.TryGetValue(id, out var lastVersion);
                    JetSemanticVersion.TryParse(version, out var thisVersion);
                    if (thisVersion == null || (lastVersion != null && lastVersion >= thisVersion))
                        continue;

                    EditorPackageDetails editorPackageDetails = null;
                    myGlobalManifest?.Packages.TryGetValue(id, out editorPackageDetails);
                    var minimumVersionString = editorPackageDetails?.MinimumVersion ?? string.Empty;
                    dependencies[id] = JetSemanticVersion.TryParse(minimumVersionString, out var minimumAllowedVersion)
                                       && minimumAllowedVersion > thisVersion
                        ? minimumAllowedVersion
                        : thisVersion;
                }
            }

            // Now find all the packages for all of those dependencies
            var newPackages = new List<PackageData>();
            foreach (var (id, version) in dependencies)
            {
                newPackages.Add(GetPackageData(id, version.ToString(), registry, builtInPackagesFolder, null));
            }

            return newPackages;
        }
    }
}
