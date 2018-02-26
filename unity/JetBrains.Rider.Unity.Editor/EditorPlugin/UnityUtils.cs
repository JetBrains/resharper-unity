using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Util.Logging;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class UnityUtils
  {
    private static readonly ILog ourLogger = Log.GetLog("UnityUtils");

    /// <summary>
    /// Force Unity To Write Project File
    /// </summary>
    public static void SyncSolution()
    {
      var T = Type.GetType("UnityEditor.SyncVS,UnityEditor");
      var syncSolution = T.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);
      syncSolution.Invoke(null, null);
    }

    internal static Version UnityVersion
    {
      get
      {
        var ver = Application.unityVersion.Split(".".ToCharArray()).Take(2).Aggregate((a, b) => a + "." + b);
        return new Version(ver);
      }
    }
  }
}