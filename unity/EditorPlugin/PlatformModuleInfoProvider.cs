using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Base;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public static class PlatformModuleInfoProvider
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(PlatformModuleInfoProvider));

    public static void Advise(Lifetime modelLifetime, BackendUnityModel model)
    {
      model.UnityPlatformInfo.Set(new UnityPlatformInfo(GetActiveBuildTarget(), GetModules(),
        GetBuildTargetGroupsWithAppIcons()));
    }

    private static string GetActiveBuildTarget()
    {
      return EditorUserBuildSettings.activeBuildTarget.ToString();
    }

    private static List<string> GetModules()
    {
      try
      {
        // Get the ModuleManager type using reflection
        var moduleManager = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Modules.ModuleManager");

        if (moduleManager == null)
          return new List<string>();

        var platformSupportModules =
          moduleManager.GetProperty("platformSupportModules", BindingFlags.Static | BindingFlags.NonPublic);

        if (platformSupportModules == null)
          return new List<string>();

        var dictionary = platformSupportModules.GetValue(null) as IDictionary;
        var valueKeys = dictionary.Keys;

        var modules = new List<string>(valueKeys.Cast<string>());
        return modules;
      }
      catch (Exception e)
      {
        ourLogger.Error(e);
        return new List<string>();
      }
    }

    private static List<string> GetBuildTargetGroupsWithAppIcons()
    {
      try
      {
        var buildTargetGroups = Enum.GetValues(typeof(BuildTargetGroup)).Cast<BuildTargetGroup>().Distinct();
        return buildTargetGroups.Where(HasAppIcons).Select(x => x.ToString()).ToList();
      }
      catch (Exception e)
      {
        ourLogger.Error(e);
        return new List<string>();
      }
    }

    private static bool HasAppIcons(BuildTargetGroup buildTargetGroup)
    {
      try
      {
        var icons = GetPlatformIcons(buildTargetGroup);

        if (icons.Length == 0)
          return false;

        // Loop through and log details about each icon
        for (var i = 0; i < icons.Length; i++)
        {
          var icon = icons[i];
          if (icon != null)
            return true;
        }

        return false;
      }
      catch (Exception e)
      {
        ourLogger.Error(e);
        return false;
      }
    }

    private static PlatformIcon[] GetPlatformIcons(BuildTargetGroup buildTargetGroup)
    {
      try
      {
        var supportedIconKinds = PlayerSettings.GetSupportedIconKindsForPlatform(buildTargetGroup);
        var icons = supportedIconKinds
          .SelectMany(s => PlayerSettings.GetPlatformIcons(buildTargetGroup, s))
          .ToArray();
        return icons;
      }
      catch
      {
        return EmptyArray<PlatformIcon>.Instance;
      }
    }
  }
}