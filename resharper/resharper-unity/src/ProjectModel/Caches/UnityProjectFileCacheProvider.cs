using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Lifetimes;
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

            var versionFromDll = TryGetUnityVersionFromDll(documentElement);

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

            return new UnityProjectDataCache(versionFromDll ?? unityVersion, explicitLangVersion);
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

        [CanBeNull]
        private static Version TryGetUnityVersionFromDll(XmlElement documentElement)
        {
            var referencePathElement = documentElement.ChildElements()
                .Where(a => a.Name == "ItemGroup").SelectMany(b => b.ChildElements())
                .Where(c => c.Name == "Reference" && c.GetAttribute("Include").StartsWith("UnityEngine") || c.GetAttribute("Include").Equals("UnityEditor"))
                .SelectMany(d => d.ChildElements())
                .FirstOrDefault(c => c.Name == "HintPath");

            if (referencePathElement == null || string.IsNullOrEmpty(referencePathElement.InnerText))
                return null;

            var filePath = FileSystemPath.Parse(referencePathElement.InnerText);
            if (!filePath.IsAbsolute) // RIDER-21237
                return null;
            
            if (filePath.ExistsFile)
            {
                if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
                {
                    var exePath = filePath.Combine("../../../Unity.exe"); // Editor\Data\Managed\UnityEngine.dll
                    if (!exePath.ExistsFile)
                        exePath = filePath.Combine("../../../../Unity.exe"); // Editor\Data\Managed\UnityEngine\UnityEngine.dll
                    if (exePath.ExistsFile)
                        return UnityVersion.ReadUnityVersionFromExe(exePath);
                }
                else if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX)
                {
                    var infoPlistPath = filePath.Combine("../../Info.plist");
                    if (!infoPlistPath.ExistsFile)
                        infoPlistPath = filePath.Combine("../../../Info.plist");
                    if (!infoPlistPath.ExistsFile)
                        return null;
                    return UnityVersion.GetVersionFromInfoPlist(infoPlistPath);
                }
            }

            return null;
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