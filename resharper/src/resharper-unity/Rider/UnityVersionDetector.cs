using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityVersionDetector
    {
        private readonly ISolution mySolution;
        private readonly ILogger myLog;

        /// <summary>
        /// Detects Unity version based on ProjectVersion.txt.
        /// Works only for Unity-generated projects.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public Version GetUnityVersion()
        {
            var defaultVersion = new Version(0, 0);
            var projectSettingsFolder = mySolution.SolutionFilePath.Directory.CombineWithShortName(ProjectExtensions.ProjectSettingsFolder);
            var projectVersionFile = projectSettingsFolder.Combine("ProjectVersion.txt");
            if (!projectVersionFile.ExistsFile)
                return defaultVersion;
            string line;

            try
            {
                var unityVersionString = "0.0";
                projectVersionFile.ReadTextStream(s =>
                {
                    while ((line = s.ReadLine()) != null)
                    {
                        if (line.StartsWith("m_EditorVersion:"))
                            unityVersionString = line.Substring("m_EditorVersion:".Length).Trim();
                    }
                });

                if (string.IsNullOrEmpty(unityVersionString))
                    return defaultVersion;

                var shortUnityVersionString = unityVersionString.Split(".".ToCharArray()).Take(2)
                    .Aggregate((a, b) => a + "." + b);
                return new Version(shortUnityVersionString);
            }
            catch (Exception e)
            {
                myLog.Log(LoggingLevel.ERROR, $"Failed to parse UnityVersion from {projectVersionFile}", e);
            }

            return defaultVersion;
        }
        
        public UnityVersionDetector(ISolution solution, ILogger log)
        {
            mySolution = solution;
            myLog = log;
        }
    }
}