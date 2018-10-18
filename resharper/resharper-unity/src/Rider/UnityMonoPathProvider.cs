using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.Runtime;
using JetBrains.Util;
using JetBrains.Util.Interop;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ShellComponent]
    public class UnityMonoPathProvider : IMonoPathProvider
    {
        private readonly ILogger myLogger;

        public UnityMonoPathProvider(ILogger logger)
        {
            myLogger = logger;
        }

        public List<FileSystemPath> GetPossibleMonoPaths()
        {
            switch (PlatformUtil.RuntimePlatform)
            {
                case PlatformUtil.Platform.MacOsX:
                {
                    var monoFolders = new List<FileSystemPath>();
                    var home = FileSystemPath.Parse("/Applications");
                    var unityDirs = home.GetChildDirectories("Unity*");
                    monoFolders.AddRange(unityDirs.Select(unityDir =>
                        unityDir.Combine(@"Unity.app/Contents/MonoBleedingEdge")));
                    
                    monoFolders.AddRange(unityDirs.Select(unityDir =>
                        unityDir.Combine(@"Unity.app/Contents/Frameworks/MonoBleedingEdge")));
                    
                    // /Applications/Unity/Hub/Editor/2018.1.0b4/Unity.app
                    monoFolders.AddRange(home.Combine("Unity/Hub/Editor").GetChildDirectories().Select(unityDir =>
                        unityDir.Combine(@"Unity.app/Contents/MonoBleedingEdge")));

                    return monoFolders;
                }
                case PlatformUtil.Platform.Linux:
                {
                    var monoFolders = new List<FileSystemPath>();
                    var home = Environment.GetEnvironmentVariable("HOME");
                    
                    // ReSharper disable once UseObjectOrCollectionInitializer
                    var homes = new List<FileSystemPath>();
                    homes.Add(FileSystemPath.Parse("/opt"));
                    if (!string.IsNullOrEmpty(home))
                        homes.Add(FileSystemPath.Parse(home));

                    monoFolders.AddRange(
                    homes.SelectMany(a => a.GetChildDirectories("Unity*"))
                        .Select(unityDir => unityDir.Combine(@"Editor/Data/MonoBleedingEdge")));
                    
                    return monoFolders;
                }

                case PlatformUtil.Platform.Windows:
                {
                    var monoFolders = new List<FileSystemPath>();
                    var programFiles = FileSystemPath.Parse(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
                    // ReSharper disable once UseObjectOrCollectionInitializer
                    var homes = new List<FileSystemPath>();
                    homes.Add(programFiles.Parent.Combine("Program Files"));
                    homes.Add(programFiles.Parent.Combine("Program Files (x86)"));
                    monoFolders.AddRange(
                        homes.SelectMany(a=>a.GetChildDirectories("Unity*")).Select(unityDir => unityDir.Combine(@"Editor\Data\MonoBleedingEdge"))
                        );
                    
                    //"C:\Program Files\Unity\Hub\Editor\2018.1.0b4\Editor\Data\MonoBleedingEdge" 
                    monoFolders.AddRange(
                        homes.SelectMany(a=>a.Combine(@"Unity\Hub\Editor").GetChildDirectories().Select(unityDir => unityDir.Combine(@"Editor\Data\MonoBleedingEdge")))
                    );
                    
                    var lnks = FileSystemPath.Parse(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs")
                        .GetChildDirectories("Unity*").SelectMany(a => a.GetChildFiles("Unity.lnk")).ToArray();
                    monoFolders.AddRange(lnks
                        .Select(a => ShellLinkHelper.ResolveLinkTarget(a).Directory.Combine(@"Data\MonoBleedingEdge"))
                        .OrderBy(c => new FileInfo(c.FullPath).CreationTime));

                    return monoFolders;
                }
            }
            myLogger.Error("Unknown runtime platfrom");
            return new List<FileSystemPath>();
        }

        public int GetPriority()
        {
            return 50;
        }
    }
}