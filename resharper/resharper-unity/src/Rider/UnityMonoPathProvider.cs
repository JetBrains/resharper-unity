using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.ReSharper.Host.Features.Runtime;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ShellComponent]
    public class UnityMonoPathProvider : IMonoPathProvider
    {
        private readonly ILogger myLogger;
        private readonly UnityInstallationFinder myInstallationFinder;

        public UnityMonoPathProvider(ILogger logger, UnityInstallationFinder installationFinder)
        {
            myLogger = logger;
            myInstallationFinder = installationFinder;
        }

        public List<FileSystemPath> GetPossibleMonoPaths()
        {
            var possibleApplicationPaths = myInstallationFinder.GetPossibleApplicationPaths();
            switch (PlatformUtil.RuntimePlatform)
            {
                case PlatformUtil.Platform.MacOsX:
                {
                    var monoFolders = possibleApplicationPaths.Select(a => a.Combine("Contents/MonoBleedingEdge")).ToList();
                    monoFolders.AddRange(possibleApplicationPaths.Select(a => a.Combine("Contents/Frameworks/MonoBleedingEdge")));
                    return monoFolders;
                }
                case PlatformUtil.Platform.Linux:
                case PlatformUtil.Platform.Windows:
                {
                    return possibleApplicationPaths.Select(a => a.Directory.Combine(@"Data/MonoBleedingEdge")).ToList();
                }
            }
            myLogger.Error("Unknown runtime platform");
            return new List<FileSystemPath>();
        }

        public int GetPriority()
        {
            return 50;
        }
    }
}