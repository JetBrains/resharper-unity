using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class UnityGameEngineDirectiveResolver : IGameEngineIncludeDirectiveResolver
    {
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly ILogger myLogger;
        private readonly object myLockObject = new object();
        private readonly ConcurrentDictionary<string, string> myVersionsFromDirectories = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> myPackageLockPaths = new ConcurrentDictionary<string, string>();
        private bool myCacheInitialized;

        public UnityGameEngineDirectiveResolver(ISolution solution, UnitySolutionTracker solutionTracker, ILogger logger)
        {
            mySolution = solution;
            mySolutionTracker = solutionTracker;
            myLogger = logger;
        }

        public bool IsApplicable(CppInclusionContext context, string path)
        {
            return path.StartsWith("Packages/") &&
                   (mySolutionTracker.IsUnityProject.HasTrueValue() ||
                    mySolutionTracker.HasUnityReference.HasTrueValue());
        }

        public string TransformPath(CppInclusionContext context, string path)
        {
            var solutionFolder = mySolution.SolutionDirectory;
            if (solutionFolder.Combine(path).Exists != FileSystemPath.Existence.Missing)
                return path;

            var pos = path.IndexOf('/') + 1;
            if (pos == -1)
                return path;
            var endPos = path.IndexOf('/', pos);
            if (endPos == -1)
                endPos = path.Length;

            var packageName = path.Substring(pos, endPos - pos);

            var packagePath = GetPackagePath(packageName);
            if (packagePath == null)
                return path;

            return packagePath + path.Substring(endPos);
        }

        private void ParsePackageLock()
        {
            try
            {
                var packagesLockPath = mySolution.SolutionDirectory.Combine("Packages").Combine("packages-lock.json");
                if (packagesLockPath.Exists == FileSystemPath.Existence.File)
                {
                    var json = JObject.Parse(packagesLockPath.ReadAllText2().Text);
                    var dependencies = json["dependencies"]?.AsArray();
                    if (dependencies != null)
                    {
                        foreach (var dependency in dependencies)
                        {
                            if (!(dependency is JProperty jProperty))
                                continue;
                            var packageName = jProperty.Name;
                            var source = (jProperty.Value["source"] as JValue)?.Value as string;
                            var version = (jProperty.Value["version"] as JValue)?.Value as string;
                            if (source == null || version == null)
                                continue;

                            if (source.Equals("embedded") || source.Equals("local"))
                            {
                                var packagePath = VirtualFileSystemPath.TryParse(version.RemoveStart("file:"), InteractionContext.SolutionContext);
                                if (packagePath.IsEmpty)
                                    continue;

                                if (packagePath.IsAbsolute) // should be true for local only
                                {
                                    myPackageLockPaths[packageName] = packagePath.FullPath;
                                }
                                else
                                {
                                    var relativePackagePath = VirtualFileSystemPath
                                        .Parse("Packages", InteractionContext.SolutionContext)
                                        .Combine(packagePath.AssertRelative());
                                    myPackageLockPaths[packageName] = relativePackagePath.FullPath;
                                }
                            }
                            else if (source.Equals("registry"))
                            {
                                var cachedPackagePath = VirtualFileSystemPath
                                    .Parse("Library", InteractionContext.SolutionContext)
                                    .Combine("PackageCache")
                                    .Combine(packageName + "@" + version);
                                myPackageLockPaths[packageName] = cachedPackagePath.FullPath;
                            } else if (source.Equals("git"))
                            {
                                var hash = (jProperty.Value["hash"] as JValue)?.Value as string;
                                if (hash == null)
                                    continue;

                                var cachedPackagePath = VirtualFileSystemPath.Parse("Library", InteractionContext.SolutionContext).Combine("PackageCache")
                                    .Combine(packageName + "@" + hash.Substring(0, Math.Min(hash.Length, 10)));
                                myPackageLockPaths[packageName] = cachedPackagePath.FullPath;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                myLogger.Error(e, "An error occurred while parsing package-lock.json");
            }
        }

        [CanBeNull]
        private string GetPackagePath(string packageName)
        {
            // TODO: This cache should be based on resolved packages, like we have in the frontend for PackageManager
            // TODO: Invalidate this cache when Packages/manifest.json or Packages/packages-lock.json changes
            if (!myCacheInitialized)
            {
                lock (myLockObject)
                {
                    if (!myCacheInitialized)
                    {

                        try
                        {
                            ParsePackageLock();
                            ProcessPackagesDirectories();
                        }
                        finally
                        {
                            myCacheInitialized = true;
                        }
                    }
                }
            }

            if (myPackageLockPaths.TryGetValue(packageName, out var path))
                return path;

            if (!myVersionsFromDirectories.TryGetValue(packageName, out var version))
                return null;

            var solutionFolder = mySolution.SolutionDirectory;
            var localPackagePath = VirtualFileSystemPath.Parse("Packages", InteractionContext.SolutionContext)
                .Combine(packageName + "@" + version).AssertRelative();
            if (solutionFolder.Combine(localPackagePath).Exists == FileSystemPath.Existence.File)
                return localPackagePath.FullPath;

            var cachedPackagePath = VirtualFileSystemPath.Parse("Library", InteractionContext.SolutionContext).Combine("PackageCache")
                .Combine(packageName + "@" + version);
            return cachedPackagePath.FullPath;
        }

        private void ProcessPackagesDirectories()
        {
            try
            {
                var solutionFolder = mySolution.SolutionDirectory;
                var packagesFolder = solutionFolder.Combine("Packages");
                var packagesCacheFolder = solutionFolder.Combine("Library").Combine("PackageCache");

                var candidates = new[] {packagesFolder, packagesCacheFolder};
                foreach (var candidate in candidates)
                {
                    foreach (var folder in candidate.GetDirectoryEntries())
                    {
                        if (!folder.IsDirectory)
                            continue;

                        // Cache entries are usually name@version, but git and local tarball packages don't
                        // have versions, so are name@hash and name@hash-timestamp, respectively. Packages
                        // in the Packages folder don't usually have a version, but can do, and things will
                        // still work, so try and find one
                        var nameAndSuffix = folder.RelativePath.Name.Split('@');
                        if (nameAndSuffix.Length != 2)
                            continue;

                        var name = nameAndSuffix[0];
                        var suffix = nameAndSuffix[1];

                        // If the suffix is a version, use it to pick the latest version. If not, pick an
                        // arbitrary cache entry. There is no guarantee that either method gets the right
                        // cache entry, but it's the best we can do without proper package details
                        if (JetSemanticVersion.TryParse(suffix, out var version) &&
                            myVersionsFromDirectories.TryGetValue(name, out var oldSuffix) &&
                            JetSemanticVersion.TryParse(oldSuffix, out var oldVersion) &&
                            oldVersion > version)
                        {
                            continue;
                        }

                        myVersionsFromDirectories[name] = suffix;
                    }
                }
            }
            catch (Exception e)
            {
                myLogger.Error(e, "Exception during calculating unity shader include paths");
            }
        }
    }
}
