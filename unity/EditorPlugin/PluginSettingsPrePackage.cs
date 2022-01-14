using System;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
    public partial class PluginSettings
    {
        /// <summary>
        /// Preferences menu layout
        /// </summary>
        /// <remarks>
        /// Contains all 3 toggles: Enable/Disable; Debug On/Off; Writing Launch File On/Off
        /// </remarks>
        [PreferenceItem("Rider")]
        private static void RiderPreferencesItem()
        {
            EditorGUIUtility.labelWidth = 200f;
            EditorGUILayout.BeginVertical();

            var alternatives = RiderPathLocator.GetAllFoundInfos(SystemInfoRiderPlugin.operatingSystemFamily);
            if (alternatives.Any()) // from known locations
            {
                var paths = alternatives.Select(a => a.Path).ToList();
                var externalEditor = EditorPrefsWrapper.ExternalScriptEditor;
                var alts = alternatives.Select(s => s.Presentation).ToList();

                if (!paths.Contains(externalEditor))
                {
                    paths.Add(externalEditor);
                    alts.Add(externalEditor);
                }

                var index = paths.IndexOf(externalEditor);


                var result = paths[EditorGUILayout.Popup("Rider build:", index == -1 ? 0 : index, alts.ToArray())];

                EditorPrefsWrapper.ExternalScriptEditor = result;
            }

            if (PluginEntryPoint.IsRiderDefaultEditor() &&
                !RiderPathProvider.RiderPathExist(EditorPrefsWrapper.ExternalScriptEditor,
                    SystemInfoRiderPlugin.operatingSystemFamily))
            {
                EditorGUILayout.HelpBox(
                    $"Rider is selected as preferred ExternalEditor, but doesn't exist on disk {EditorPrefsWrapper.ExternalScriptEditor}",
                    MessageType.Warning);
            }

            UseLatestRiderFromToolbox = EditorGUILayout.Toggle(new GUIContent("Update Rider to latest version"),
                UseLatestRiderFromToolbox);

            GUILayout.BeginVertical();
            LogEventsCollectorEnabled =
                EditorGUILayout.Toggle(new GUIContent("Pass Console to Rider:"), LogEventsCollectorEnabled);

            if (UnityUtils.ScriptingRuntime > 0)
            {
                OverrideTargetFrameworkVersion =
                    EditorGUILayout.Toggle(new GUIContent("Override TargetFrameworkVersion:"),
                        OverrideTargetFrameworkVersion);
                if (OverrideTargetFrameworkVersion)
                {
                    var help = @"TargetFramework >= 4.6 is recommended.";
                    TargetFrameworkVersion =
                        EditorGUILayout.TextField(
                            new GUIContent("For Active profile NET 4.6",
                                help), TargetFrameworkVersion);
                    EditorGUILayout.HelpBox(help, MessageType.None);
                }
            }
            else
            {
                OverrideTargetFrameworkVersionOldMono = EditorGUILayout.Toggle(
                    new GUIContent("Override TargetFrameworkVersion:"), OverrideTargetFrameworkVersionOldMono);
                if (OverrideTargetFrameworkVersionOldMono)
                {
                    var helpOldMono = @"TargetFramework = 3.5 is recommended.
 - With 4.5 Rider may show ambiguous references in UniRx.";

                    TargetFrameworkVersionOldMono =
                        EditorGUILayout.TextField(
                            new GUIContent("For Active profile NET 3.5:",
                                helpOldMono), TargetFrameworkVersionOldMono);
                    EditorGUILayout.HelpBox(helpOldMono, MessageType.None);
                }
            }

            // Unity 2018.1 doesn't require installed dotnet framework, it references everything from Unity installation
            if (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows &&
                UnityUtils.UnityVersion < new Version(2018, 1))
            {
                var detectedDotnetText = string.Empty;
                var installedFrameworks = GetInstalledNetFrameworks();
                if (installedFrameworks.Any())
                    detectedDotnetText = installedFrameworks.OrderBy(v => new Version(v))
                        .Aggregate((a, b) => a + "; " + b);
                EditorGUILayout.HelpBox($"Installed dotnet versions: {detectedDotnetText}", MessageType.None);
            }

            GUILayout.EndVertical();

            OverrideLangVersion = EditorGUILayout.Toggle(new GUIContent("Override LangVersion:"), OverrideLangVersion);
            if (OverrideLangVersion)
            {
                var workaroundUrl = "https://gist.github.com/van800/875ce55eaf88d65b105d010d7b38a8d4";
                var workaroundText = "Use this <color=#0000FF>workaround</color> if overriding doesn't work.";
                var helpLangVersion = @"Avoid overriding, unless there is no particular need.";

                LangVersion =
                    EditorGUILayout.TextField(
                        new GUIContent("LangVersion:",
                            helpLangVersion), LangVersion);
                LinkButton(caption: workaroundText, url: workaroundUrl);
                EditorGUILayout.HelpBox(helpLangVersion, MessageType.None);
            }

            GUILayout.Label("");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log file:");
            var previous = GUI.enabled;
            GUI.enabled = previous && SelectedLoggingLevel != LoggingLevel.OFF;
            var button = GUILayout.Button(new GUIContent("Open log"));
            if (button)
            {
                //UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(PluginEntryPoint.LogPath, 0);
                // works much faster than the commented code, when Rider is already started
                PluginEntryPoint.OpenAssetHandler.OnOpenedAsset(PluginEntryPoint.LogPath, 0, 0);
            }

            GUI.enabled = previous;
            GUILayout.EndHorizontal();

            var loggingMsg =
                @"Sets the amount of Rider Debug output. If you are about to report an issue, please select Verbose logging level and attach Unity console output to the issue.";
            SelectedLoggingLevel =
                (LoggingLevel)EditorGUILayout.EnumPopup(new GUIContent("Logging Level:", loggingMsg),
                    SelectedLoggingLevel);

            EditorGUILayout.HelpBox(loggingMsg, MessageType.None);

            // This setting is natively supported in Unity 2018.2+
            if (UnityUtils.UnityVersion < new Version(2018, 2))
            {
                EditorGUI.BeginChangeCheck();
                AssemblyReloadSettings =
                    (ScriptCompilationDuringPlay)EditorGUILayout.EnumPopup("Script Changes during Playing:",
                        AssemblyReloadSettings);

                if (EditorGUI.EndChangeCheck())
                {
                    if (AssemblyReloadSettings == ScriptCompilationDuringPlay.RecompileAfterFinishedPlaying &&
                        EditorApplication.isPlaying)
                    {
                        ourLogger.Info("LockReloadAssemblies");
                        EditorApplication.LockReloadAssemblies();
                    }
                    else
                    {
                        ourLogger.Info("UnlockReloadAssemblies");
                        EditorApplication.UnlockReloadAssemblies();
                    }
                }
            }

            var githubRepo = "https://github.com/JetBrains/resharper-unity";
            var caption = $"<color=#0000FF>{githubRepo}</color>";
            LinkButton(caption: caption, url: githubRepo);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            GUILayout.Label("Plugin version: " + version,
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = new Color(0, 0, 0, .6f), },
                    margin = new RectOffset(4, 4, 4, 4),
                });

            GUILayout.EndHorizontal();

            // left for testing purposes
/*      if (GUILayout.Button("reset RiderInitializedOnce = false"))
      {
        RiderInitializedOnce = false;
      }*/

            EditorGUILayout.EndVertical();
        }
    }
}