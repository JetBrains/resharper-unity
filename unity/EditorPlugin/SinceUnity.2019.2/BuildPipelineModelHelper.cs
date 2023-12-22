using System.IO;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.PathLocator;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public static class BuildPipelineModelHelper
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(BuildPipelineModelHelper));

    public static void Advise(Lifetime modelLifetime, BackendUnityModel model)
    {
      // Get the root directory of the Android SDK tools, if available. Can return null to indicate that the tools are
      // not installed
      model.GetAndroidSdkRoot.Set(_ =>
      {
        // ReSharper disable once JoinDeclarationAndInitializer
        string sdkRoot;

#if UNITY_2019_2_OR_NEWER
        // TODO: This method was internal until 2018.1, possibly with different parameters
        if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
          // This is the checkbox in Unity Settings (External Tools) to use the Android SDK Tools that ships with Unity
          var useEmbedded = EditorPrefs.GetBool("SdkUseEmbedded");
          ourLogger.Verbose("Android build target installed. Use embedded Android SDK flag: {0}", useEmbedded);
          if (useEmbedded)
          {
            // TODO: This method was internal until 2019.1
            // Note that calling it when the build target isn't installed will write an error to the Unity Console
            sdkRoot = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None);
            ourLogger.Verbose("Using embedded Android SDK Tools: {0}", sdkRoot ?? "(Not installed)");
            return sdkRoot;
          }
        }
        else
          ourLogger.Verbose("Android build target not installed");
#endif

        // Check if the Android SDK root has been set manually
        sdkRoot = EditorPrefs.GetString("AndroidSdkRoot");
        ourLogger.Verbose("Custom Android SDK Tools: {0}", sdkRoot ?? "(Not set)");

        if (string.IsNullOrEmpty(sdkRoot))
        {
          // Fall back to known Android build target install location. If the build target is installed but we don't
          // have the API to check, either it will be in the custom directory above, or in the default location. The
          // default location is `{EditorApplication.applicationContentsPath}/PlaybackEngines/AndroidPlayer`. This is
          // true for Windows, but not Mac, where `applicationContentsPath` is inside the `Unity.app` distribution,
          // which can't be modified for code signing reasons.
          // On mac, the path is `{EditorApplication.applicationContentsPath}/../../PlaybackEngines`. Note that the
          // Documentation folder is the same, but `Unity.app/Contents/Documentation` is a link to the external folder.
          // * macOS: /Applications/Unity/Hub/Editor/2022.2.3f1/PlaybackEngines/AndroidPlayer
          // * Windows: C:\Program Files\Unity\Hub\Editor\2022.2.3f1\Editor\Data\PlaybackEngines\AndroidPlayer
          var unityAppBaseDataFolder =
            PluginSettings.SystemInfoRiderPlugin.OS == OS.MacOSX
              ? Path.GetFullPath(Path.Combine(Path.GetFullPath(EditorApplication.applicationPath), ".."))
              : Path.GetFullPath(EditorApplication.applicationContentsPath);
          sdkRoot = Path.Combine(unityAppBaseDataFolder, "PlaybackEngines/AndroidPlayer");
        }

        return sdkRoot;
      });
    }
  }
}