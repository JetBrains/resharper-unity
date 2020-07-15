using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class UnityGameEngineDirectiveResolver : IGameEngineIncludeDirectiveResolver
    {
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker mySolutionTracker;
        private readonly UnityReferencesTracker myReferencesTracker;
        private readonly ILogger myLogger;

        public UnityGameEngineDirectiveResolver(ISolution solution, UnitySolutionTracker solutionTracker, UnityReferencesTracker referencesTracker, ILogger logger)
        {
            mySolution = solution;
            mySolutionTracker = solutionTracker;
            myReferencesTracker = referencesTracker;
            myLogger = logger;
        }
        
        public bool IsApplicable(CppInclusionContext context, string path)
        {
              return path.StartsWith("Packages/") &&
                   (mySolutionTracker.IsUnityProject.HasTrueValue() || myReferencesTracker.HasUnityReference.HasTrueValue());
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

            var version = GetVersionFor(packageName);
            var localPackagePath = mySolution.SolutionDirectory.Combine("Packages").Combine(packageName + "@" + version + path.Substring(endPos));
            if (localPackagePath.Exists == FileSystemPath.Existence.File)
                return localPackagePath.FullPath;
            
            var cachedPackagePath = mySolution.SolutionDirectory.Combine("Library").Combine("PackageCache").Combine(packageName + "@" + version + path.Substring(endPos));
            return cachedPackagePath.FullPath;
        }

        private readonly object myLockObject = new object();
        // TODO, add package version tracker
        private ConcurrentDictionary<string, JetSemanticVersion> myPackageVersions = null;
        
        [NotNull]
        private JetSemanticVersion GetVersionFor(string packageName)
        {
            if (myPackageVersions == null)
            {
                lock (myLockObject)
                {
                    if (myPackageVersions == null)
                    {
                        var result = new ConcurrentDictionary<string, JetSemanticVersion>();
                        try
                        {
                            var solDir = mySolution.SolutionDirectory;
                            var packagesFolder = solDir.Combine("Packages");
                            var packagesCacheFolder = solDir.Combine("Library").Combine("PackageCache");

                            var candidates = new[] {packagesFolder, packagesCacheFolder};
                            foreach (var candidate in candidates)
                            {
                                foreach (var folder in candidate.GetDirectoryEntries())
                                {
                                    if (!folder.IsDirectory)
                                        continue;
                                        
                                    var packageAndVersion = folder.RelativePath.Name.Split('@');
                                    if (packageAndVersion.Length != 2)
                                        continue;

                                    var version = JetSemanticVersion.Parse(packageAndVersion[1]);
                                    if (result.TryGetValue(packageAndVersion[0], out var oldVersion) && oldVersion > version)
                                        continue;

                                    result[packageAndVersion[0]] = version;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            myLogger.Error(e, "Exception during calculating unity shader include paths");
                        }
                        finally
                        {
                            myPackageVersions = result;
                        }
                    }
                }
            }

            return myPackageVersions.GetValueSafe(packageName);
        }
    }
}