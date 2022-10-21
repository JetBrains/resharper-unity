using System;
using System.Text.RegularExpressions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml
{
    public class ProjectSettingsAsset
    {
        public static Rd.Tasks.RdTask<int> GetScriptingBackend(ISolution solution, ILogger logger)
        {
            // ScriptingImplementation 
            // 0 Mono
            // 1 IL2CPP
            // 2 WinRTDotNET
            
            // read from `ProjectSettings/ProjectSettings.asset`
            // scriptingBackend:
            //   Standalone: 0

            try
            {
                var solutionDir = solution.SolutionDirectory;
                if (!solutionDir.IsAbsolute) return Rd.Tasks.RdTask<int>.Faulted(new InvalidOperationException("solutionDir.IsAbsolute")); // True in tests
                var settingsPath = solutionDir.Combine("ProjectSettings/ProjectSettings.asset");
                if (!settingsPath.ExistsFile)
                    return Rd.Tasks.RdTask<int>.Faulted(new InvalidOperationException($"{settingsPath} ExistsFile is false."));
                var fileIsInText = settingsPath.SniffYamlHeader();
                if (!fileIsInText)
                    return Rd.Tasks.RdTask<int>.Faulted(new InvalidOperationException($"{settingsPath} is not serialized to Text."));

                var text = settingsPath.ReadAllText2().Text;
                var match = Regex.Match(text, @"scriptingBackend:\s*$\s*^\s*Standalone:\s+(?<mode>\d+)\s*$", RegexOptions.Multiline);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups["mode"].Value, out var mode))
                    {
                        var task = new Rd.Tasks.RdTask<int>();
                        task.Set(mode);
                        return task;
                    }
                    logger.Warn($"Unable to parse scriptingBackend mode from ${settingsPath}");
                }
                else
                    logger.Warn($"Unable to parse scriptingBackend from ${settingsPath}");
            }
            catch(Exception e)
            {
                return Rd.Tasks.RdTask<int>.Faulted(e);
            }
            
            return Rd.Tasks.RdTask<int>.Faulted(new InvalidOperationException("GetScriptingBackend failed."));
        }
    }
}