using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;

[SolutionComponent(Instantiation.ContainerAsyncAnyThreadSafe)]
public class UnityPackageProjectResolution
{
    // - Starting with Unity 6000.0.22f1, the package installation folder names have been changed from <packageName> to <packageName>@<fingerprint>.substring(0,12)
    // fingerprint can be obtained from "Library/PackageManager/projectResolution.json",
    // however this file is prone to future changes, so we plan to add some our own json, written by the Rider package

    private readonly VirtualFileSystemPath myProjectResolutionPath;
    private readonly ILogger myLogger;
    
    private DateTime myLastModifiedTime;
    private readonly Dictionary<string, string> myFingerprintsDictionary;
    
    public UnityPackageProjectResolution(ISolution solution, ILogger logger)
    {
        myLogger = logger;
        myProjectResolutionPath = solution.SolutionDirectory.Combine("Library").Combine("PackageManager").Combine("projectResolution.json");
        myFingerprintsDictionary = new Dictionary<string, string>();
        myLastModifiedTime = DateTime.MinValue;
    }


    public string GetFingerprint(string key)
    {
        if (key == null) return string.Empty;
        if (!myProjectResolutionPath.ExistsFile) return string.Empty;

        var lastWriteTime = myProjectResolutionPath.FileModificationTimeUtc;
        if (lastWriteTime > myLastModifiedTime)
        {
            InvalidateCache();
            myLastModifiedTime = lastWriteTime;
        }

        return myFingerprintsDictionary.TryGetValue(key, out var fingerprint) ? fingerprint : string.Empty;
    }

    private void InvalidateCache()
    {
        myFingerprintsDictionary.Clear();
        try
        {
            myProjectResolutionPath.ReadStream(stream =>
            {
                using var rawReader = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(rawReader);
                var jsonObj = JObject.Load(jsonReader);

                if (jsonObj.SelectToken("$.outputs") is not JObject outputs) return;
                foreach (var property in outputs.Properties())
                {
                    var fingerprintToken = property.Value["fingerprint"];
                    if (fingerprintToken == null) continue;
                    myFingerprintsDictionary[property.Name] = fingerprintToken.ToString()[..12];
                }
            });
        }
        catch (Exception e)
        {
            myLogger.Error(e, $"Failed to build a cache on {myProjectResolutionPath}");
        }
    }
}