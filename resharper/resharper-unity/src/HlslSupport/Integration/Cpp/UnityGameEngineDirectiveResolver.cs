using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Cpp
{
    [SolutionComponent]
    public class UnityGameEngineDirectiveResolver : IGameEngineIncludeDirectiveResolver
    {
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly UnityReferencesTracker myReferencesTracker;
        private readonly ILogger myLogger;
        private readonly object myLockObject = new object();
        private ConcurrentDictionary<string, string> myCacheSuffixes;

        public UnityGameEngineDirectiveResolver(ISolution solution, UnitySolutionTracker solutionTracker,
                                                UnityReferencesTracker referencesTracker, ILogger logger)
        {
            mySolution = solution;
            mySolutionTracker = solutionTracker;
            myReferencesTracker = referencesTracker;
            myLogger = logger;
        }

        public bool IsApplicable(CppInclusionContext context, string path)
        {
            return path.StartsWith("Packages/") &&
                   (mySolutionTracker.IsUnityProject.HasTrueValue() ||
                    myReferencesTracker.HasUnityReference.HasTrueValue());
        }

        public string TransformPath(CppInclusionContext context, string path)
        {
            var pos = path.IndexOf('/') + 1;
            if (pos == -1)
                return path;
            var endPos = path.IndexOf('/', pos);
            if (endPos == -1)
                endPos = path.Length;

            var packageName = path.Substring(pos, endPos - pos);

            var suffix = GetCacheSuffix(packageName);
            var localPackagePath = mySolution.SolutionDirectory.Combine("Packages")
                .Combine(packageName + "@" + suffix + path.Substring(endPos));
            if (localPackagePath.Exists == FileSystemPath.Existence.File)
                return localPackagePath.FullPath;

            var cachedPackagePath = mySolution.SolutionDirectory.Combine("Library").Combine("PackageCache")
                .Combine(packageName + "@" + suffix + path.Substring(endPos));
            return cachedPackagePath.FullPath;
        }

        [NotNull]
        private string GetCacheSuffix(string packageName)
        {
            // TODO: This cache should be based on resolved packages, like we have in the frontend for PackageManager
            // TODO: Invalidate this cache when Packages/manifest.json or Packages/packages-lock.json changes
            if (myCacheSuffixes == null)
            {
                lock (myLockObject)
                {
                    if (myCacheSuffixes == null)
                    {
                        var result = new ConcurrentDictionary<string, string>();
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
                                        result.TryGetValue(name, out var oldSuffix) &&
                                        JetSemanticVersion.TryParse(oldSuffix, out var oldVersion) &&
                                        oldVersion > version)
                                    {
                                        continue;
                                    }

                                    result[name] = suffix;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            myLogger.Error(e, "Exception during calculating unity shader include paths");
                        }
                        finally
                        {
                            myCacheSuffixes = result;
                        }
                    }
                }
            }

            return myCacheSuffixes.GetValueSafe(packageName);
        }
    }
}