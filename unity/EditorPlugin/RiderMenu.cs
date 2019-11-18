using System.Linq;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  // MenuOpenProject is also used to start Rider automatically via Unity commandline switches
  public static class RiderMenu
  {
    // The default "Open C# Project" menu item will use the external script editor to load the .sln
    // file, but unless Unity knows the external script editor can properly load solutions, it will
    // also launch MonoDevelop (or the OS registered app for .sln files). This menu item side steps
    // that issue, and opens the solution in Rider without opening MonoDevelop as well.
    // Unity 2017.1 and later recognise Rider as an app that can load solutions, so this menu isn't
    // needed in newer versions.
    [MenuItem("Assets/Open C# Project in Rider", false, 1000)]
    public static void MenuOpenProject()
    {
      // method can be called via commandline
      // Force the project files to be sync
      UnityUtils.SyncSolution();

      // Load Project
      PluginEntryPoint.CallRider(string.Format("{0}{1}{0}", "\"", PluginEntryPoint.SlnFile));
    }

    [MenuItem("Assets/Open C# Project in Rider", true, 1000)]
    public static bool ValidateMenuOpenProject()
    {
      var model = PluginEntryPoint.UnityModels.FirstOrDefault(a => a.Lifetime.IsAlive);
      if (model == null)
        return true;
      return false;
    }

    /// <summary>
    /// Forces regeneration of .csproj / .sln files.
    /// </summary>
    [MenuItem("Assets/Sync C# Project", false, 1001)]
    private static void MenuSyncProject()
    {
      UnityUtils.SyncSolution();
    }

    [MenuItem("Assets/Sync C# Project", true, 1001)]
    private static bool ValidateMenuSyncProject()
    {
      return true;
    }
  }
}