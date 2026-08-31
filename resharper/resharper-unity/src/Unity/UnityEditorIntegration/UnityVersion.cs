using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Feature.Services.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public partial class UnityVersion : IUnityReferenceChangeHandler, IUnityVersion
    {
        public const string VersionRegex = @"(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)(?<type>[a-z])(?<revision>\d+)";

        private readonly UnityProjectFileCacheProvider myUnityProjectFileCache;
        private readonly ISolution mySolution;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly VirtualFileSystemPath mySolutionDirectory;
        private IReadonlyProperty<Version> mySolutionWideVersion;
        private readonly ViewableProperty<VirtualFileSystemPath> myAppPathFromLastAddedProject = new();

        private static readonly ILogger ourLogger = Logger.GetLogger<UnityVersion>();

        public ViewableProperty<Version> ActualVersionForSolution { get; } = new(new Version(0,0));
        public readonly ViewableProperty<VirtualFileSystemPath> ActualAppPathForSolution = new();

        public UnityVersion(UnityProjectFileCacheProvider unityProjectFileCache,
            ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime,
            UnitySolutionTracker unitySolutionTracker)
        {
            myUnityProjectFileCache = unityProjectFileCache;
            mySolution = solution;
            myFileSystemTracker = fileSystemTracker;

            // SolutionDirectory isn't absolute in tests, and will throw if used with FileSystemTracker
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory().ToVirtualFileSystemPath());

            var needsUnityHandling = unitySolutionTracker.IsUnityProjectFolder.Compose(lifetime, unitySolutionTracker.HasUnityReference, (a, b) => a || b);
            needsUnityHandling.WhenTrueOnce(lifetime, InitializeUnityVersionProperties);
        }

        private void InitializeUnityVersionProperties(Lifetime lt)
        {
            var projectVersionTxtPath = UnityVersionUtils.GetProjectVersionPath(mySolutionDirectory);
            var projectVersion = CreatePropertyFromPath(projectVersionTxtPath, lt, _ =>
            {
                var version = UnityVersionUtils.GetProjectSettingsUnityVersion(mySolutionDirectory);
                return version == null ? null : Parse(version);
            });
            
            var editorInstanceJsonPath = mySolutionDirectory.Combine("Library/EditorInstance.json");
            var editorInstanceJson = CreatePropertyFromPath(editorInstanceJsonPath, lt, EditorInstanceJson.TryRead);

            mySolutionWideVersion = editorInstanceJson.Compose(lt, projectVersion, (editorInstanceData, versionProjectVersionTxt) =>
            {
                if (editorInstanceData != null && editorInstanceData.TryGetValue("version", out var versionString))
                    return Parse(versionString);
                
                return versionProjectVersionTxt;
            });
            
            mySolutionWideVersion.Advise(lt, version =>
            {
                if (version == null) version = FindFallbackVersionForSolution();
                ourLogger.Verbose($"Setting ActualVersionForSolution to {version}");
                ActualVersionForSolution.SetValue(version);
            });
            
            // When Unity MSBuild compilation is enabled, it creates the unitylocation.txt that we can use
            var unityLocationTxtPath = mySolutionDirectory.Combine("Library/MSBuild/unitylocation.txt");
            var appPathFromUnityLocationTxt = CreatePropertyFromPath(unityLocationTxtPath, lt, TryGetAppPathFromUnityLocationTxt);

            editorInstanceJson
                .Compose(lt, myAppPathFromLastAddedProject, (editorInstanceData, pathFromLastAddedProject) =>
                {
                    if (editorInstanceData != null && editorInstanceData.TryGetValue("app_path", out var pathString))
                        return VirtualFileSystemPath.Parse(pathString, InteractionContext.SolutionContext);

                    return pathFromLastAddedProject;
                })
                .Compose(lt, appPathFromUnityLocationTxt, (path, pathFromUnityLocationTxt) =>
                {
                    // NOTE: we compose the unitylocation.txt value last, because if user disables the msbuild compilation
                    // pipeline, the file will be left over, never updated. at the same time the last-added-project path
                    // will become valid (non-null), since our project generation code will go into effect and produce
                    // project files from which we can extract the path to the editor
                    ActualAppPathForSolution.SetValue(path ?? pathFromUnityLocationTxt);
                });
        }
        
        private IReadonlyProperty<T> CreatePropertyFromPath<T>(VirtualFileSystemPath path, Lifetime lt,
            Func<VirtualFileSystemPath, T> createValue)
        {
            var property = new ViewableProperty<T>(createValue(path));
            myFileSystemTracker.AdviseFileChanges(lt, path, _ => property.SetValue(createValue(path)));
            return property;
        }

        private static VirtualFileSystemPath TryGetAppPathFromUnityLocationTxt(VirtualFileSystemPath unityLocationTxtPath)
        {
            if (!unityLocationTxtPath.ExistsFile)
                return null;

            var pathString = unityLocationTxtPath.ReadAllText2(Encoding.UTF8).Text;
            var path = VirtualFileSystemPath.Parse(pathString, InteractionContext.SolutionContext);
            return UnityInstallationFinder.FindUnityAppPath(path);
        }

        [NotNull]
        public Version GetActualVersion([CanBeNull] IProject project)
        {
            var solutionWideVersion = mySolutionWideVersion?.Maybe.ValueOrDefault;
            if (solutionWideVersion != null) return solutionWideVersion;

            // Project might be null for e.g. decompiled files
            if (project == null) return new Version(0, 0);

            return myUnityProjectFileCache.GetUnityVersion(project) ?? FindFallbackVersionForSolution();
        }

        [NotNull]
        private Version FindFallbackVersionForSolution()
        {
            if (mySolution.IsVirtualSolution())
                return new Version(0, 0);

            foreach (var project in GetTopLevelProjectWithReadLock(mySolution))
            {
                var version = myUnityProjectFileCache.GetUnityVersion(project);
                if (version != null)
                    return version;
            }

            return GetVersionForTests(mySolution);
        }

        public VirtualFileSystemPath GetActualAppPathForSolution()
        {
            if (mySolution.IsVirtualSolution())
                return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);

            var appPath = ActualAppPathForSolution.Maybe.ValueOrDefault;
            if (!appPath.IsNullOrEmpty())
                return appPath;

            ourLogger.Verbose(
                "UnityVersion.GetActualAppPathForSolution is empty path. May happen for a regular project with a reference to UnityEditor.dll outside of Unity installation.");
            return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);
        }

        private static Version GetVersionForTests(ISolution solution)
        {
            // The project file data provider/cache doesn't work in tests, because there is no .csproj file we can parse.
            // Instead, pull the version directly from the project defines in the project model. We can't rely on this
            // as our main strategy because Unity doesn't write defines for Release configuration (another reason we for
            // us to hide the project configuration selector)
            var unityVersion = new Version(0, 0);
            foreach (var project in GetTopLevelProjectWithReadLock(solution))
            {
                foreach (var configuration in project.ProjectProperties.GetActiveConfigurations<IManagedProjectConfiguration>())
                {
                    // Get the version define from the project configuration, if set. The solution might be initialised
                    // before the test aspect attribute has a chance to update the project configuration, so fall back
                    // to the properties collection.
                    var defineConstants = configuration.DefineConstants ?? string.Empty;
                    unityVersion = UnityProjectFileCacheProvider.GetVersionFromDefines(defineConstants, unityVersion);
                    if (unityVersion.Major == 0)
                    {
                        configuration.PropertiesCollection.TryGetValue("DefineConstants", out var defineConstantsProp);
                        unityVersion = UnityProjectFileCacheProvider.GetVersionFromDefines(defineConstantsProp ?? string.Empty, unityVersion);
                    }
                }
            }

            return unityVersion;
        }

        private static ICollection<IProject> GetTopLevelProjectWithReadLock(ISolution solution)
        {
            ICollection<IProject> projects;
            using (ReadLockCookie.Create())
            {
                projects = solution.GetTopLevelProjects();
            }

            return projects;
        }

        [CanBeNull]
        public static Version Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var match = Regex.Match(input, VersionRegex);
            var groups = match.Groups;
            Version version = null;
            if (match.Success)
            {
                var typeWithRevision = "0";
                try
                {
                    var typeChar = groups["type"].Value.ToCharArray()[0];
                    var shiftedChar = 16 + typeChar; // Because `f1` = `1021` and `b10` = `9810`, which will break sorting
                    var revision = Convert.ToInt32(groups["revision"].Value);
                    typeWithRevision = shiftedChar.ToString("D3") + revision.ToString("D3");
                }
                catch (Exception e)
                {
                    ourLogger.Error($"Unable to parse part of version. type={groups["type"].Value} revision={groups["revision"].Value}", e);
                }

                version = Version.Parse($"{groups["major"].Value}.{groups["minor"].Value}.{groups["build"].Value}.{typeWithRevision}");
            }

            return version;
        }

        public static string VersionToString([NotNull] Version version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            var type = string.Empty;
            var rev = string.Empty;
            try
            {
                var revisionString = version.Revision.ToString(); // first 3 is char, next 1+ ones - revision
                if (revisionString.Length > 3)
                {
                    var charValue = Convert.ToInt32(revisionString.Substring(0, 3)) - 16;
                    type = ((char)charValue).ToString();
                    rev = Convert.ToInt32(revisionString.Substring(3)).ToString();
                }
            }
            catch (Exception e)
            {
                ourLogger.Error($"Unable do VersionToString. Input version={version}", e);
            }

            var build = version.Build >= 0 ? $".{version.Build}" : string.Empty;
            return $"{version.Major}.{version.Minor}{build}{type}{rev}";
        }

        public static bool RequiresRiderPackage(Version version)
        {
            return version >= new Version(2019,2);
        }

        void IUnityReferenceChangeHandler.OnHasUnityReference()
        {
            // do nothing
        }

        void IUnityReferenceChangeHandler.OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
            myAppPathFromLastAddedProject.SetValue(myUnityProjectFileCache.GetAppPath(project));
        }
    }
}