using System;
using System.Linq;
using JetBrains.Application.FileSystemTracker;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityVersion
    {
        private readonly UnityProjectFileCacheProvider myUnityProjectFileCache;
        private readonly ILogger myLog;
        private readonly Lifetime myLifetime;
        private readonly IFileSystemTracker myFileSystemTracker;
        private Version myCachedUnityVersion;

        public UnityVersion(UnityProjectFileCacheProvider unityProjectFileCache, ILogger log, Lifetime lifetime, IFileSystemTracker fileSystemTracker)
        {
            myUnityProjectFileCache = unityProjectFileCache;
            myLog = log;
            myLifetime = lifetime;
            myFileSystemTracker = fileSystemTracker;
        }

        public Version GetActualVersion(IProject project)
        {
            var version = myUnityProjectFileCache.GetUnityVersion(project);
            return version ?? GetActualVersion(project.GetSolution());
        }

        public Version GetActualVersion(ISolution solution)
        {
            if (myCachedUnityVersion!=null)
              return myCachedUnityVersion;  
            
            foreach (var project in solution.GetTopLevelProjects())
            {
                if (project.IsUnityProject())
                {
                    var version = myUnityProjectFileCache.GetUnityVersion(project);
                    if (version != null)
                    {
                        myCachedUnityVersion = version;
                        return version;
                    }
                }
            }
            
            // Tests don't create a .csproj we can parse, so pull the version out
            // of the project defines directly (we can't do this normally because
            // Unity doesn't write defines for Release configuration, so we can't
            // rely on this)
            foreach (var project in solution.GetTopLevelProjects())
            {
                foreach (var configuration in project.ProjectProperties.GetActiveConfigurations<IManagedProjectConfiguration>())
                {
                    // Get the constants. The tests can't set this up correctly, so they
                    // add the Unity version as a property
                    var defineConstants = configuration.DefineConstants;
                    if (string.IsNullOrEmpty(defineConstants))
                        configuration.PropertiesCollection.TryGetValue("DefineConstants", out defineConstants);

                    myCachedUnityVersion = UnityProjectFileCacheProvider.GetVersionFromDefines(defineConstants ?? string.Empty,
                        myCachedUnityVersion);
                }
            }
            if (myCachedUnityVersion != null)
                return myCachedUnityVersion;
            
            myCachedUnityVersion = GetVersionByProjectVersionFile(solution);
            if (myCachedUnityVersion != null)
                return myCachedUnityVersion;
            
            // If all else fails, default to 5.4. No reason for that version, other
            // than it was the first supported version :)
            return new Version(5, 4);
        }

        private Version GetVersionByProjectVersionFile(ISolution solution)
        {
            var projectSettingsFolder = solution.SolutionFilePath.Directory.CombineWithShortName(ProjectExtensions.ProjectSettingsFolder);
            var projectVersionFile = projectSettingsFolder.Combine("ProjectVersion.txt");
            myFileSystemTracker.AdviseFileChanges(myLifetime, projectVersionFile,
                delta => { myCachedUnityVersion = GetVersionByProjectVersionFileInternal(projectVersionFile); });
            
            myCachedUnityVersion = GetVersionByProjectVersionFileInternal(projectVersionFile);
            return myCachedUnityVersion;
        }

        private Version GetVersionByProjectVersionFileInternal(FileSystemPath projectVersionFile)
        {
            if (!projectVersionFile.ExistsFile)
                return null;
            string line;

            try
            {
                var unityVersionString = string.Empty;
                projectVersionFile.ReadTextStream(s =>
                {
                    while ((line = s.ReadLine()) != null)
                    {
                        if (line.StartsWith("m_EditorVersion:"))
                            unityVersionString = line.Substring("m_EditorVersion:".Length).Trim();
                    }
                });

                if (string.IsNullOrEmpty(unityVersionString))
                    return null;

                var shortUnityVersionString = unityVersionString.Split(".".ToCharArray()).Take(2)
                    .Aggregate((a, b) => a + "." + b);
                return new Version(shortUnityVersionString);
            }
            catch (Exception e)
            {
                myLog.Error($"Failed to parse UnityVersion from {projectVersionFile}", e);
            }

            return null;
        }

    }
}