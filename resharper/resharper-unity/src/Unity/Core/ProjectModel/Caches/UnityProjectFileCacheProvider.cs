using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Caches
{
    [SolutionComponent(Instantiation.ContainerAsyncAnyThreadSafe)]
    public class UnityProjectFileCacheProvider : IProjectFileDataProvider<UnityProjectDataCache>
    {
        private static readonly char[] ourSymbolSeparator = { ';', ',' };
        private static readonly Regex ourVersionRegex = new Regex(@"UNITY_(?<major>\d+)_(?<minor>\d+)(_(?<build>\d+))?");

        private readonly ISolution mySolution;
        private readonly IProjectFileDataCache myCache;
        private readonly Dictionary<VirtualFileSystemPath, Action> myCallbacks;

        public UnityProjectFileCacheProvider(Lifetime lifetime, ISolution solution, IProjectFileDataCache cache)
        {
            mySolution = solution;
            myCache = cache;

            myCache.RegisterCache(lifetime, this);
            myCallbacks = new Dictionary<VirtualFileSystemPath, Action>();
        }
        
        // be aware that LangVersion can also be in a custom NET SDK or Directory.Build.props
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

        [CanBeNull]
        public VirtualFileSystemPath GetAppPath([NotNull] IProject project)
        {
            var data = myCache.GetData(this, project);
            return data?.UnityAppPath;
        }

        public bool CanHandle(VirtualFileSystemPath projectFileLocation)
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

        public int Version => 3;

        public UnityProjectDataCache Read(VirtualFileSystemPath projectFileLocation, BinaryReader reader)
        {
            var version = System.Version.Parse(reader.ReadString());
            var explicitlySpecified = reader.ReadBoolean();
            var unityAppPathString = reader.ReadNullableString();
            var unityAppPath = unityAppPathString != null ? VirtualFileSystemPath.Parse(unityAppPathString, InteractionContext.SolutionContext) : null;
            return new UnityProjectDataCache(version, explicitlySpecified, unityAppPath);
        }

        public void Write(VirtualFileSystemPath projectFileLocation, BinaryWriter writer, UnityProjectDataCache data)
        {
            writer.Write(data.UnityVersion.ToString());
            writer.Write(data.LangVersionExplicitlySpecified);
            writer.WriteNullableString(data.UnityAppPath?.FullPath);
        }

        public UnityProjectDataCache BuildData(VirtualFileSystemPath projectFileLocation, XmlDocument document)
        {
            var documentElement = document.DocumentElement;
            if (documentElement == null || documentElement.Name != "Project")
                return UnityProjectDataCache.Empty;

            var explicitLangVersion = false;
            var unityVersion = new Version(0, 0);

            var appPath = UnityInstallationFinder.GetAppPathByDll(documentElement);
            var versionFromDll = UnityVersion.GetVersionByAppPath(appPath);

            foreach (XmlNode propertyGroup in documentElement.GetElementsByTagName("PropertyGroup"))
            {
                var xmlElement = propertyGroup as XmlElement;
                if (xmlElement == null)
                    continue;

                // We can't just grab the value here because there may be multiple values set, one per configuration.
                // I haven't seen Unity or Rider do this, so it must be VSTU, but I don't have proof...
                if (xmlElement.GetElementsByTagName("LangVersion").Count > 0)
                    explicitLangVersion = true;

                if (versionFromDll != null)
                    continue;

                // Ideally, we could get the defines through the project model (see IManagedProjectConfiguration), but
                // that only seems to give us the currently active project settings, and Unity's own .csproj creator
                // only sets the version for the Debug build, not the Release build. VSTU sets the defines in both
                // configurations.
                foreach (XmlNode defines in xmlElement.GetElementsByTagName("DefineConstants"))
                    unityVersion = GetVersionFromDefines(defines.InnerText, unityVersion);
            }

            return new UnityProjectDataCache(versionFromDll ?? unityVersion, explicitLangVersion, appPath);
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
                    var newVersion = int.TryParse(match.Groups["build"].Value, out var build) ? new Version(major, minor, build) : new Version(major, minor);
                    if (newVersion > unityVersion)
                        unityVersion = newVersion;
                }
            }
            return unityVersion;
        }

        public Action OnDataChanged(VirtualFileSystemPath projectFileLocation, UnityProjectDataCache oldData, UnityProjectDataCache newData)
        {
            myCallbacks.TryGetValue(projectFileLocation, out var action);
            return action;
        }
    }

    public class UnityProjectDataCache
    {
        public static readonly UnityProjectDataCache Empty = new UnityProjectDataCache(new Version(0, 0), false, VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext));

        public UnityProjectDataCache(Version unityVersion, bool langVersionExplicitlySpecified, VirtualFileSystemPath unityAppPath)
        {
            UnityVersion = unityVersion;
            UnityAppPath = unityAppPath;
            LangVersionExplicitlySpecified = langVersionExplicitlySpecified;
        }

        public Version UnityVersion { get; }
        public VirtualFileSystemPath UnityAppPath { get; }
        public bool LangVersionExplicitlySpecified { get; }
    }
}