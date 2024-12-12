#nullable enable
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
    // - Starting with Unity 6000.1, the package folder changed
    // Built-in Data/Resources/PackageManager/BuiltInPackages/package_id
    // Registry Library/PackageCaches/package_id@fingerprint
    // both can be read the same way from "projectResolution.json"
    // however this file is prone to future changes, so we plan to add some our own json, written by the Rider package
    
    private class Data(string? id, VirtualFileSystemPath resolvedPath, PackageSource packageSource)
    {
        public readonly string? ID = id;
        public readonly VirtualFileSystemPath ResolvedPath = resolvedPath;
        public readonly PackageSource Source = packageSource;
    }

    private readonly VirtualFileSystemPath myProjectResolutionPath;
    private readonly ILogger myLogger;

    private DateTime myLastModifiedTime;
    private readonly Dictionary<string, Data> myPackages;

    public UnityPackageProjectResolution(ISolution solution, ILogger logger)
    {
        myLogger = logger;
        myProjectResolutionPath = solution.SolutionDirectory.Combine("Library").Combine("PackageManager").Combine("projectResolution.json");
        myPackages = [];
        myLastModifiedTime = DateTime.MinValue;
    }

    private void InvalidateCache()
    {
        myPackages.Clear();
        try
        {
            myProjectResolutionPath.ReadStream(stream =>
            {
                using var rawReader  = new StreamReader(stream);
                using var jsonReader = new JsonTextReader(rawReader);
                var jsonObj = JObject.Load(jsonReader);

                if (jsonObj.SelectToken("$.outputs") is not JObject outputs) return;
                foreach (var property in outputs.Properties())
                {
                    var id = property.Value["name"]?.ToString();
                    var resolvedPathToken = property.Value["resolvedPath"];
                    if (resolvedPathToken == null) continue;
                    var source = property.Value["source"]?.ToString();
                    // property.Name in the json looks like a composite key of name and version, where version can be file:path for local and tarbal package types
                    myPackages[property.Name] = new Data(id, VirtualFileSystemPath.TryParse(resolvedPathToken.ToString(), InteractionContext.Local), PackageSourceExtensions.ToPackageSource(source));
                }
            });
        }
        catch (Exception e)
        {
            myLogger.Error(e, $"Failed to build a cache on {myProjectResolutionPath}");
        }
    }

    public List<PackageData>? GetPackages()
    {
        try
        {
            if (!myProjectResolutionPath.ExistsFile)
            {
                myLogger.Verbose("packageResolution.json does not exist");
                return null;
            }
            
            myLogger.Info("Attempt to use projectResolution.json to determine packages");
            
            var lastWriteTime = myProjectResolutionPath.FileModificationTimeUtc;
            if (lastWriteTime > myLastModifiedTime)
            {
                InvalidateCache();
                myLastModifiedTime = lastWriteTime;
            }

            var packages = new Dictionary<string, PackageData>();
            foreach (var package in myPackages.Values)
            {

                var packageData = PackageData.GetFromFolder(package.ID, package.ResolvedPath, package.Source);
                if (packageData != null)
                {
                    packages[packageData.Id] = packageData;
                }
            }

            // there should not be several versions of one package,
            // but we wouldn't fail, even if there iss.
            return [..packages.Values];
        }
        catch (Exception e)
        {
            myLogger.LogExceptionSilently(e);
            return null;
        }
    }
}