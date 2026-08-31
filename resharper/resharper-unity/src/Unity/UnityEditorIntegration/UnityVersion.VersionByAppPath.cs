using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Diagnostics;
using JetBrains.HabitatDetector;
using JetBrains.Util;
using Vestris.ResourceLib;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;

public partial class UnityVersion
{
    private static readonly ConcurrentDictionary<VirtualFileSystemPath, Version> ourUnityPathToVersion = new();

    public static Version GetVersionByAppPath(VirtualFileSystemPath appPath)
    {
        if (appPath == null || appPath.Exists == FileSystemPath.Existence.Missing)
            return null;

        return ourUnityPathToVersion.GetOrAdd(appPath, GetVersionByAppPathInternal);
    }

    private static Version GetVersionByAppPathInternal(VirtualFileSystemPath appPath)
    {
        Version version = null;
        ourLogger.CatchWarn(() => // RIDER-23674
        {
            switch (PlatformUtil.RuntimePlatform)
            {
                case JetPlatform.Windows:

                    ourLogger.CatchWarn(() =>
                    {
                        var fileVersion = FileVersionInfo.GetVersionInfo(appPath.FullPath).FileVersion;
                        if (!string.IsNullOrEmpty(fileVersion))
                            version = Version.Parse(Version.Parse(fileVersion).ToString(3));
                    });

                    var resource = new VersionResource();
                    resource.LoadFrom(appPath.FullPath);
                    var unityVersionList = resource.Resources.Values.OfType<StringFileInfo>()
                        .Where(c => c.Default.Strings.Keys.Any(b => b == "Unity Version")).ToArray();
                    if (unityVersionList.Any())
                    {
                        var unityVersion = unityVersionList.First().Default.Strings["Unity Version"].StringValue;
                        version = Parse(unityVersion);
                    }

                    break;
                case JetPlatform.MacOsX:
                    var infoPlistPath = appPath.Combine("Contents/Info.plist");
                    if (infoPlistPath.ExistsFile)
                    {
                        var docs = XDocument.Load(infoPlistPath.FullPath);
                        var keyValuePairs = docs.Descendants("dict")
                            .SelectMany(d => d.Elements("key").Zip(d.Elements().Where(e => e.Name != "key"),
                                (k, v) => new { Key = k, Value = v }))
                            .GroupBy(x => x.Key.Value)
                            .Select(g =>
                                g.First()) // avoid exception An item with the same key has already been added.
                            .ToDictionary(i => i.Key.Value, i => i.Value.Value);
                        version = Parse(keyValuePairs["CFBundleVersion"]);
                    }

                    break;
                case JetPlatform.Linux:
                    version = Parse(appPath.FullPath); // parse from path
                    break;
            }
        });
        return version;
    }
}