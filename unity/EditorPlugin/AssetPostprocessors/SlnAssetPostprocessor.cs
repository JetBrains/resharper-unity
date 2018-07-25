using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public class SlnAssetPostprocessor : AssetPostprocessor
  {
    private static readonly ILog ourLogger = Log.GetLog<SlnAssetPostprocessor>();

    public override int GetPostprocessOrder()
    {
      return 10;
    }

    // This method is new for 2017.4. It allows multiple processors to modify the contents of the generated .csproj in
    // memory, and Unity will only write to disk if it's different to the existing file. It's safe for pre-2017.4 as it
    // simply won't get called
    [UsedImplicitly]
    public static string OnGeneratedSlnSolution(string path, string content)
    {
      if (!PluginEntryPoint.Enabled)
      {
        ourLogger.Verbose("OnGeneratedSlnSolution: Not updating solution file - plugin not enabled");
        return content;
      }

      ourLogger.Verbose("Post-processing {0} (in memory)", path);
      try
      {
        return ProcessSlnText(content);
      }
      catch (Exception e)
      {
        Debug.LogError(e);
        ourLogger.Error(e, "OnGeneratedSlnSolution");
        return content;
      }
    }

    public static void OnGeneratedCSProjectFiles()
    {
      if (!PluginEntryPoint.Enabled)
      {
        ourLogger.Verbose("OnGeneratedCSProjectFiles: Not updating solution files - plugin not enabled");
        return;
      }

      if (UnityUtils.UnityVersion >= new Version(2017, 4))
      {
        ourLogger.Verbose("OnGeneratedCSProjectFiles: Not updating files - legacy method");
        return;
      }

      try
      {
        var slnFile = PluginEntryPoint.SlnFile;
        if (!File.Exists(slnFile))
          return;

        ourLogger.Verbose("Post-processing {0}", slnFile);
        var slnAllText = File.ReadAllText(slnFile);
        var text = ProcessSlnText(slnAllText);
        File.WriteAllText(slnFile, text);
      }
      catch (Exception e)
      {
        // unhandled exception kills editor
        Debug.LogError(e);
        ourLogger.Error(e, "OnGeneratedCSProjectFiles");
      }
    }

    public static string ProcessSlnText(string slnAllText)
    {
      const string csharpProjectGuid = @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"")";
      if (!slnAllText.Contains(csharpProjectGuid))
      {
        const string matchGuid = @"Project\(\""\{[A-Z0-9]{8}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{12}\}\""\)";
        // Unity (possibly only 5.1) can insert an incorrect GUID, which prevents the project loading correctly
        // Make sure we use the standard C# project system GUID
        // See https://rider-support.jetbrains.com/hc/en-us/community/posts/207243685-Unity3D-support?page=1#community_comment_208602469
        // And https://youtrack.jetbrains.com/issue/RIDER-1261 (demo project shows type in C# guid: FA*A*04EC0-301F-11D3-BF4B-00C04F79EFBC)
        slnAllText = Regex.Replace(slnAllText, matchGuid, csharpProjectGuid);
      }

      var lines = slnAllText.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
      var sb = new StringBuilder();
      foreach (var line in lines)
      {
        if (line.StartsWith("Project("))
        {
          var mc = Regex.Matches(line, "\"([^\"]*)\"");
          //RiderPlugin.Log(RiderPlugin.LoggingLevel.Info, "mc[1]: "+mc[1].Value);
          //RiderPlugin.Log(RiderPlugin.LoggingLevel.Info, "mc[2]: "+mc[2].Value);
          var to = GetFileNameWithoutExtension(mc[2].Value.Substring(1, mc[2].Value.Length - 2)); // remove quotes
          //RiderPlugin.Log(RiderPlugin.LoggingLevel.Info, "to:" + to);
          //RiderPlugin.Log(RiderPlugin.LoggingLevel.Info, line);
          var newLine = line.Substring(0, mc[1].Index + 1) + to + line.Substring(mc[1].Index + mc[1].Value.Length - 1);
          sb.Append(newLine);
          //RiderPlugin.Log(RiderPlugin.LoggingLevel.Info, newLine);
        }
        else
        {
          sb.Append(line);
        }

        sb.Append(Environment.NewLine);
      }

      return sb.ToString();
    }

    private static string GetFileNameWithoutExtension(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      int length;
      return (length = path.LastIndexOf('.')) == -1 ? path : path.Substring(0, length);
    }

    internal static string[] GetCsprojLinesInSln()
    {
      var slnFile = PluginEntryPoint.SlnFile;
      if (!File.Exists(slnFile))
        return new string[0];

      var slnAllText = File.ReadAllText(slnFile);
      var lines = slnAllText.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
        .Where(a => a.StartsWith("Project(")).ToArray();
      return lines;
    }
  }
}