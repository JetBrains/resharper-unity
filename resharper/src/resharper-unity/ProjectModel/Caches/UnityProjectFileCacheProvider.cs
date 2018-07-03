using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches
{
    [SolutionComponent]
    public class UnityProjectFileCacheProvider : IProjectFileDataProvider<UnityProjectDataCache>
    {
        private static readonly char[] ourSymbolSeparator = { ';', ',' };
        private static readonly Regex ourVersionRegex = new Regex(@"UNITY_(?<major>\d+)_(?<minor>\d+)");

        private readonly ISolution mySolution;
        private readonly IProjectFileDataCache myCache;
        private readonly Dictionary<FileSystemPath, Action> myCallbacks;

        public UnityProjectFileCacheProvider(Lifetime lifetime, ISolution solution, IProjectFileDataCache cache)
        {
            mySolution = solution;
            myCache = cache;

            myCache.RegisterCache(lifetime, this);
            myCallbacks = new Dictionary<FileSystemPath, Action>();
        }

        public void RegisterDataChangedCallback(Lifetime lifetime, FileSystemPath projectLocation, Action action)
        {
            // Make sure we have a valid project file location to key off. This will be empty for e.g. solution folders,
            // Misc project and most importantly, tests
            if (!projectLocation.IsEmpty)
                myCallbacks.Add(lifetime, projectLocation, action);
        }

        public bool IsLangVersionExplicitlySpecified(IProject project)
        {
            var data = myCache.GetData(this, project);
            return data != null && data.LangVersionExplicitlySpecified;
        }

        [CanBeNull]
        public Version GetUnityVersion([NotNull] IProject project)
        {
            var data = myCache.GetData(this, project);
            return data?.UnityVersion;
        }

        public bool CanHandle(FileSystemPath projectFileLocation)
        {
            using (ReadLockCookie.Create())
            {
                foreach (var projectItem in mySolution.FindProjectItemsByLocation(projectFileLocation))
                {
                    var projectFile = projectItem as IProjectFile;
                    var project = projectFile?.GetProject();
                    if (project?.ProjectProperties.BuildSettings is CSharpBuildSettings) return true;
                }
                return false;
            }
        }

        public int Version => 1;

        public UnityProjectDataCache Read(FileSystemPath projectFileLocation, BinaryReader reader)
        {
            var version = System.Version.Parse(reader.ReadString());
            var explicitlySpecified = reader.ReadBoolean();
            return new UnityProjectDataCache(version, explicitlySpecified);
        }

        public void Write(FileSystemPath projectFileLocation, BinaryWriter writer, UnityProjectDataCache data)
        {
            writer.Write(data.UnityVersion.ToString());
            writer.Write(data.LangVersionExplicitlySpecified);
        }

        public UnityProjectDataCache BuildData(FileSystemPath projectFileLocation, XmlDocument document)
        {
            var documentElement = document.DocumentElement;
            if (documentElement == null || documentElement.Name != "Project")
                return UnityProjectDataCache.Empty;

            var explicitLangVersion = false;
            var unityVersion = new Version(0, 0);

            foreach (XmlNode propertyGroup in documentElement.GetElementsByTagName("PropertyGroup"))
            {
                var xmlElement = propertyGroup as XmlElement;
                if (xmlElement == null)
                    continue;

                if (xmlElement.GetElementsByTagName("LangVersion").Count > 0)
                {
                    explicitLangVersion = true;
                }

                // Ideally, we could get the defines through the project model
                // (see IManagedProjectConfiguration), but that only seems to
                // give us the currently active project settings, and Unity's
                // own .csproj creator only sets the version for the Debug build,
                // not the Release build. VSTU sets the defines in both configurations.
                foreach (XmlNode defines in xmlElement.GetElementsByTagName("DefineConstants"))
                    unityVersion = GetVersionFromDefines(defines.InnerText, unityVersion);
            }

            return new UnityProjectDataCache(unityVersion, explicitLangVersion);
        }

        public static Version GetVersionFromDefines(string defines, [NotNull] Version unityVersion)
        {
            foreach (var constant in defines.Split(ourSymbolSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var name = constant.Trim();
                var match = ourVersionRegex.Match(name);
                if (match.Success)
                {
                    var major = int.Parse(match.Groups["major"].Value);
                    var minor = int.Parse(match.Groups["minor"].Value);

                    // TODO: Perhaps we should also capture maintenance version
                    // If we do, we need to update API range checks, because those are only major/minor, and the actual
                    // version (e.g. 2018.1.1) would be larger than the API version (2018.1)

                    var newVersion = new Version(major, minor);
                    if (newVersion > unityVersion)
                        unityVersion = newVersion;
                }
            }
            return unityVersion;
        }

        public Action OnDataChanged(FileSystemPath projectFileLocation, UnityProjectDataCache oldData, UnityProjectDataCache newData)
        {
            myCallbacks.TryGetValue(projectFileLocation, out var action);
            return action;
        }
    }

    public class UnityProjectDataCache
    {
        public static readonly UnityProjectDataCache Empty = new UnityProjectDataCache(new Version(0, 0), false);

        public UnityProjectDataCache(Version unityVersion, bool langVersionExplicitlySpecified)
        {
            UnityVersion = unityVersion;
            LangVersionExplicitlySpecified = langVersionExplicitlySpecified;
        }

        public Version UnityVersion { get; }
        public bool LangVersionExplicitlySpecified { get; }
    }
}