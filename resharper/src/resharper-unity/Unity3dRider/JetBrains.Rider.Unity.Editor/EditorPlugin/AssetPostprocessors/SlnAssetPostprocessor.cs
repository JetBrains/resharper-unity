using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Util.Logging;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public class SlnAssetPostprocessor : AssetPostprocessor
  {
    private static readonly ILog ourLogger = Log.GetLog<SlnAssetPostprocessor>();
    
    public static void OnGeneratedCSProjectFiles()
    {
      if (!PluginEntryPoint.Enabled)
        return;
     
      var slnFile = PluginEntryPoint.SlnFile;
      if (!File.Exists(slnFile))
        return;
      
      ourLogger.Verbose("Post-processing {0}", slnFile);
      var slnAllText = File.ReadAllText(slnFile);
      const string unityProjectGuid = @"Project(""{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1}"")";
      if (!slnAllText.Contains(unityProjectGuid))
      {
        var matchGuid = @"Project\(\""\{[A-Z0-9]{8}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{12}\}\""\)";
        // Unity may put a random guid, unityProjectGuid will help VSTU recognize Rider-generated projects
        slnAllText = Regex.Replace(slnAllText, matchGuid, unityProjectGuid);
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
          var to = GetFileNameWithoutExtension(mc[2].Value.Substring(1, mc[2].Value.Length-1)); // remove quotes
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
      File.WriteAllText(slnFile, sb.ToString());
    }
    
    private static string GetFileNameWithoutExtension(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      int length;
      return (length = path.LastIndexOf('.')) == -1 ? path : path.Substring(0, length);
    }

  }
}