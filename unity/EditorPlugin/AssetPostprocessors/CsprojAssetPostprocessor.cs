using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public class CsprojAssetPostprocessor : AssetPostprocessor
  {
    private static readonly ILog ourLogger = Log.GetLog<CsprojAssetPostprocessor>();

    public override int GetPostprocessOrder()
    {
      return 10;
    }

    // This method is new for 2017.4. It allows multiple processors to modify the contents of the generated .csproj in
    // memory, and Unity will only write to disk if it's different to the existing file. It's safe for pre-2017.4 as it
    // simply won't get called
    public static string OnGeneratedCSProject(string path, string contents)
    {
      ourLogger.Verbose("Post-processing {0} (in memory)", path);
      var doc = XDocument.Parse(contents);
      if (UpgradeProjectFile(path, doc))
      {
        ourLogger.Verbose("Post-processed with changes {0} (in memory)", path);
        return doc.ToString();
      }

      ourLogger.Verbose("Post-processed with NO changes {0}", path);
      return contents;
    }

    // This method is for pre-2017.4, and is called after the file has been written to disk
    public static void OnGeneratedCSProjectFiles()
    {
      if (!PluginEntryPoint.Enabled || UnityUtils.UnityVersion >= new Version(2017, 4))
        return;

      try
      {
        // get only csproj files, which are mentioned in sln
        var lines = SlnAssetPostprocessor.GetCsprojLinesInSln();
        var currentDirectory = Directory.GetCurrentDirectory();
        var projectFiles = Directory.GetFiles(currentDirectory, "*.csproj")
          .Where(csprojFile => lines.Any(line => line.Contains("\"" + Path.GetFileName(csprojFile) + "\""))).ToArray();

        foreach (var file in projectFiles)
        {
          UpgradeProjectFile(file);
        }
      }
      catch (Exception e)
      {
        // unhandled exception kills editor
        Debug.LogError(e);
      }
    }

    private static void UpgradeProjectFile(string projectFile)
    {
      ourLogger.Verbose("Post-processing {0}", projectFile);
      XDocument doc;
      try
      {
        doc = XDocument.Load(projectFile);
      }
      catch (Exception)
      {
        ourLogger.Verbose("Failed to Load {0}", projectFile);
        return;
      }

      if (UpgradeProjectFile(projectFile, doc))
      {
        ourLogger.Verbose("Post-processed with changes {0}ss", projectFile);
        doc.Save(projectFile);
        return;
      }

      ourLogger.Verbose("Post-processed with NO changes {0}", projectFile);
    }

    private static bool UpgradeProjectFile(string projectFile, XDocument doc)
    {
      var changed = false;
      doc.Changed += (sender, args) => changed = true;

      var projectContentElement = doc.Root;
      XNamespace xmlns = projectContentElement.Name.NamespaceName; // do not use var

      FixTargetFrameworkVersion(projectContentElement, xmlns);
      FixUnityEngineReference(projectContentElement, xmlns); // shouldn't be needed in Unity 2018.2
      FixSystemXml(projectContentElement, xmlns);
      SetLangVersion(projectContentElement, xmlns);
      SetProjectFlavour(projectContentElement, xmlns);

      // Unity 2017.1 and later has this features by itself
      if (UnityUtils.UnityVersion < new Version(2017, 1))
      {
        SetManuallyDefinedComilingSettings(projectFile, projectContentElement, xmlns);
      }

      SetXCodeDllReference("UnityEditor.iOS.Extensions.Xcode.dll", projectContentElement, xmlns);
      SetXCodeDllReference("UnityEditor.iOS.Extensions.Common.dll", projectContentElement, xmlns);

      ApplyManualCompilingSettingsReferences(projectContentElement, xmlns);

      return changed;
    }

    private static void FixSystemXml(XElement projectContentElement, XNamespace xmlns)
    {
      var el = projectContentElement
        .Elements(xmlns+"ItemGroup")
        .Elements(xmlns+"Reference")
        .FirstOrDefault(a => a.Attribute("Include") !=null && a.Attribute("Include").Value=="System.XML");
      if (el != null)
      {
        el.Attribute("Include").Value = "System.Xml";
      }
    }

    private static readonly string PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH = Path.GetFullPath("Assets/mcs.rsp");
    private const string UNITY_PLAYER_PROJECT_NAME = "Assembly-CSharp.csproj";
    private const string UNITY_EDITOR_PROJECT_NAME = "Assembly-CSharp-Editor.csproj";
    private const string UNITY_UNSAFE_KEYWORD = "-unsafe";
    private const string UNITY_DEFINE_KEYWORD = "-define:";
    private static readonly string  PLAYER_PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH = Path.GetFullPath("Assets/smcs.rsp");
    private static readonly string  EDITOR_PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH = Path.GetFullPath("Assets/gmcs.rsp");

    private static void SetManuallyDefinedComilingSettings(string projectFile, XElement projectContentElement, XNamespace xmlns)
    {
      string configPath = null;

      //Prefer mcs.rsp if it exists
      if (File.Exists(PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH))
      {
        configPath = PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH;
      }
      else
      {
        if (IsPlayerProjectFile(projectFile))
          configPath = PLAYER_PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH;
        else if (IsEditorProjectFile(projectFile))
          configPath = EDITOR_PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH;
      }

      if (!string.IsNullOrEmpty(configPath))
        ApplyManualCompilingSettings(configPath, projectContentElement, xmlns);
    }

    private static void ApplyManualCompilingSettings(string configFilePath, XElement projectContentElement, XNamespace xmlns)
    {
      if (File.Exists(configFilePath))
      {
        var configText = File.ReadAllText(configFilePath);
        if (configText.Contains(UNITY_UNSAFE_KEYWORD))
        {
          // Add AllowUnsafeBlocks to the .csproj. Unity doesn't generate it (although VSTU does).
          // Strictly necessary to compile unsafe code
          ApplyAllowUnsafeBlocks(projectContentElement, xmlns);
        }
        if (configText.Contains(UNITY_DEFINE_KEYWORD))
        {
          // defines could be
          // 1) -define:DEFINE1,DEFINE2
          // 2) -define:DEFINE1;DEFINE2
          // 3) -define:DEFINE1 -define:DEFINE2
          // 4) -define:DEFINE1,DEFINE2;DEFINE3
          // tested on "-define:DEF1;DEF2 -define:DEF3,DEF4;DEFFFF \n -define:DEF5"
          // result: DEF1, DEF2, DEF3, DEF4, DEFFFF, DEF5

          var definesList = new List<string>();
          var compileFlags = configText.Split(' ', '\n');
          foreach (var flag in compileFlags)
          {
            var f = flag.Trim();
            if (f.Contains(UNITY_DEFINE_KEYWORD))
            {
              var defineEndPos = f.IndexOf(UNITY_DEFINE_KEYWORD) + UNITY_DEFINE_KEYWORD.Length;
              var definesSubString = f.Substring(defineEndPos,f.Length - defineEndPos);
              definesSubString = definesSubString.Replace(";", ",");
              definesList.AddRange(definesSubString.Split(','));
            }
          }

          ApplyCustomDefines(definesList.ToArray(), projectContentElement, xmlns);
        }
      }
    }

    private static void ApplyCustomDefines(string[] customDefines, XElement projectContentElement, XNamespace xmlns)
    {
      var definesString = string.Join(";", customDefines);

      var defineConstants = projectContentElement
        .Elements(xmlns+"PropertyGroup")
        .Elements(xmlns+"DefineConstants")
        .FirstOrDefault(definesConsts=> !string.IsNullOrEmpty(definesConsts.Value));

      defineConstants?.SetValue(defineConstants.Value + ";" + definesString);
    }

    private static void ApplyAllowUnsafeBlocks(XElement projectContentElement, XNamespace xmlns)
    {
      projectContentElement.AddFirst(
        new XElement(xmlns + "PropertyGroup", new XElement(xmlns + "AllowUnsafeBlocks", true)));
    }

    private static bool IsPlayerProjectFile(string projectFile)
    {
      return Path.GetFileName(projectFile) == UNITY_PLAYER_PROJECT_NAME;
    }

    private static bool IsEditorProjectFile(string projectFile)
    {
      return Path.GetFileName(projectFile) == UNITY_EDITOR_PROJECT_NAME;
    }

    private static void SetXCodeDllReference(string name, XElement projectContentElement, XNamespace xmlns)
    {
      var unityAppBaseFolder = Path.GetDirectoryName(EditorApplication.applicationPath);
      if (string.IsNullOrEmpty(unityAppBaseFolder))
      {
        ourLogger.Verbose("SetXCodeDllReference. unityAppBaseFolder IsNullOrEmpty");
        return;
      }

      var xcodeDllPath = Path.Combine(unityAppBaseFolder, Path.Combine("Data/PlaybackEngines/iOSSupport", name));
      if (!File.Exists(xcodeDllPath))
        xcodeDllPath = Path.Combine(unityAppBaseFolder, Path.Combine("PlaybackEngines/iOSSupport", name));

      if (!File.Exists(xcodeDllPath))
        return;

      var itemGroup = new XElement(xmlns + "ItemGroup");
      var reference = new XElement(xmlns + "Reference");
      reference.Add(new XAttribute("Include", Path.GetFileNameWithoutExtension(xcodeDllPath)));
      reference.Add(new XElement(xmlns + "HintPath", xcodeDllPath));
      itemGroup.Add(reference);
      projectContentElement.Add(itemGroup);
    }

    private static void FixUnityEngineReference(XElement projectContentElement, XNamespace xmlns)
    {
      var unityAppBaseFolder = Path.GetDirectoryName(EditorApplication.applicationPath);
      if (string.IsNullOrEmpty(unityAppBaseFolder))
      {
        ourLogger.Verbose("FixUnityEngineReference. unityAppBaseFolder IsNullOrEmpty");
        return;
      }

      var el = projectContentElement
        .Elements(xmlns+"ItemGroup")
        .Elements(xmlns+"Reference")
        .FirstOrDefault(a => a.Attribute("Include") !=null && a.Attribute("Include").Value=="UnityEngine");
      var hintPath = el?.Elements(xmlns + "HintPath").FirstOrDefault();
      if (hintPath == null)
        return;
      var oldUnityEngineDllFileInfo = new FileInfo(hintPath.Value);
      var unityEngineDir = new DirectoryInfo(Path.Combine(oldUnityEngineDllFileInfo.Directory.FullName, "UnityEngine"));
      if (!unityEngineDir.Exists)
        return;

      var newDllPath = Path.Combine(unityEngineDir.FullName, "UnityEngine.dll");
      if (!File.Exists(newDllPath))
        return;

      hintPath.SetValue(newDllPath);

      var files = unityEngineDir.GetFiles("*.dll");
      foreach (var file in files)
      {
        var itemGroup = new XElement(xmlns + "ItemGroup");
        var reference = new XElement(xmlns + "Reference");
        reference.Add(new XAttribute("Include", Path.GetFileNameWithoutExtension(file.Name)));
        reference.Add(new XElement(xmlns + "HintPath", file.FullName));
        itemGroup.Add(reference);
        projectContentElement.Add(itemGroup);
      }
    }

    private const string UNITY_REFERENCE_KEYWORD = "-r:";
    /// <summary>
    /// Handles custom references -r: in "mcs.rsp"
    /// </summary>
    /// <param name="projectContentElement"></param>
    /// <param name="xmlns"></param>
    private static void ApplyManualCompilingSettingsReferences(XElement projectContentElement, XNamespace xmlns)
    {
      if (!File.Exists(PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH))
        return;

      var configFilePath = PROJECT_MANUAL_CONFIG_ABSOLUTE_FILE_PATH;

      if (File.Exists(configFilePath))
      {
        var configText = File.ReadAllText(configFilePath);
        if (configText.Contains(UNITY_REFERENCE_KEYWORD))
        {
          var referenceList = new List<string>();
          var compileFlags = configText.Split(' ', '\n');
          foreach (var flag in compileFlags)
          {
            var f = flag.Trim();
            if (f.Contains(UNITY_REFERENCE_KEYWORD))
            {
              var defineEndPos = f.IndexOf(UNITY_REFERENCE_KEYWORD) + UNITY_REFERENCE_KEYWORD.Length;
              var definesSubString = f.Substring(defineEndPos,f.Length - defineEndPos);
              definesSubString = definesSubString.Replace(";", ",");
              referenceList.AddRange(definesSubString.Split(','));
            }
          }

          foreach (var referenceName in referenceList)
          {
            string hintPath = null;

            var name = referenceName;
            if (name.Substring(name.Length - 4) != ".dll")
              name += ".dll"; // RIDER-15093

            if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows)
            {
              var unityAppBaseFolder = Path.GetDirectoryName(EditorApplication.applicationPath);
              var monoDir = new DirectoryInfo(Path.Combine(unityAppBaseFolder, "MonoBleedingEdge/lib/mono"));
              if (!monoDir.Exists)
                monoDir = new DirectoryInfo(Path.Combine(unityAppBaseFolder, "Data/MonoBleedingEdge/lib/mono"));

              var newestApiDir = monoDir.GetDirectories("4.*").LastOrDefault();
              if (newestApiDir != null)
              {
                var dllPath = new FileInfo(Path.Combine(newestApiDir.FullName, name));
                if (dllPath.Exists)
                  hintPath = dllPath.FullName;
              }
            }

            ApplyCustomReference(name, projectContentElement, xmlns, hintPath);
          }
        }
      }
    }

    private static void ApplyCustomReference(string name, XElement projectContentElement, XNamespace xmlns, string hintPath = null)
    {
      var itemGroup = new XElement(xmlns + "ItemGroup");
      var reference = new XElement(xmlns + "Reference");
      reference.Add(new XAttribute("Include", Path.GetFileNameWithoutExtension(name)));
      if (!string.IsNullOrEmpty(hintPath))
        reference.Add(new XElement(xmlns + "HintPath", hintPath));
      itemGroup.Add(reference);
      projectContentElement.Add(itemGroup);
    }

    // Set appropriate version
    private static void FixTargetFrameworkVersion(XElement projectElement, XNamespace xmlns)
    {
      SetOrUpdateProperty(projectElement, xmlns, "TargetFrameworkVersion", s =>
        {
          if (string.IsNullOrEmpty(s))
          {
            ourLogger.Verbose("TargetFrameworkVersion in csproj is null or empty.");
            return string.Empty;
          }

          string version = string.Empty;
          try
          {
            version = s.Substring(1);
            // for windows try to use installed dotnet framework
            if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows)
            {
              var versions = PluginSettings.GetInstalledNetFrameworks();
              if (versions.Any())
              {
                var versionOrderedList = versions.OrderBy(v1 => new Version(v1));
                var foundVersion = UnityUtils.ScriptingRuntime > 0
                  ? versionOrderedList.Last()
                  : versionOrderedList.First();
                // Unity may require dotnet 4.7.1, which may not be present
                var fvIsParsed = VersionExtensions.TryParse(foundVersion, out var fv);
                var vIsParsed = VersionExtensions.TryParse(version, out var v);
                if (fvIsParsed && vIsParsed && (UnityUtils.ScriptingRuntime == 0 || UnityUtils.ScriptingRuntime > 0 && fv > v))
                  version = foundVersion;
                else if (foundVersion == version)
                  ourLogger.Verbose("Found TargetFrameworkVersion {0} equals the one set-by-Unity itself {1}",
                    foundVersion, version);
                else if (ourLogger.IsVersboseEnabled())
                {
                  var message = $"Rider may require \".NET Framework {version} Developer Pack\", which is not installed.";
                  Debug.Log(message);
                }
              }
            }
          }
          catch (Exception e)
          {
            ourLogger.Log(LoggingLevel.WARN, "Fail to FixTargetFrameworkVersion", e);
          }

          if (UnityUtils.ScriptingRuntime > 0)
          {
            if (PluginSettings.OverrideTargetFrameworkVersion)
            {
              return "v" + PluginSettings.TargetFrameworkVersion;
            }
          }
          else
          {
            if (PluginSettings.OverrideTargetFrameworkVersionOldMono)
            {
              return "v" + PluginSettings.TargetFrameworkVersionOldMono;;
            }
          }

          return "v" + version;
        }
      );
    }

    private static void SetLangVersion(XElement projectElement, XNamespace xmlns)
    {
      // Set the C# language level, so Rider doesn't have to guess (although it does a good job)
      // VSTU sets this, and I think newer versions of Unity do too (should check which version)
      SetOrUpdateProperty(projectElement, xmlns, "LangVersion", existing =>
      {
        if (PluginSettings.OverrideLangVersion)
        {
          return PluginSettings.LangVersion;
        }

        var expected = GetExpectedLanguageLevel();
        if (expected == "latest" || existing == "latest")
          return "latest";

        // Only use our version if it's not already set, or it's less than what we would set
        // Note that if existing is "default", we'll override it
        var currentIsParsed = VersionExtensions.TryParse(existing, out var currentLanguageLevel);
        var expectedIsParsed = VersionExtensions.TryParse(expected, out var expectedLanguageLevel);
        if (currentIsParsed && expectedIsParsed && currentLanguageLevel < expectedLanguageLevel
            || !currentIsParsed
            )
        {
          return expected;
        }

        return existing;
      });
    }

    private static string GetExpectedLanguageLevel()
    {
      // https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration/src
      if (Directory.Exists(Path.GetFullPath("CSharp70Support")))
        return "latest";
      if (Directory.Exists(Path.GetFullPath("CSharp60Support")))
        return "6";

      var apiCompatibilityLevel = 0;
      try
      {
        //PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup)
        var method = typeof(PlayerSettings).GetMethod("GetApiCompatibilityLevel");
        var parameter = typeof(EditorUserBuildSettings).GetProperty("selectedBuildTargetGroup");
        var val = parameter.GetValue(null, null);
        apiCompatibilityLevel = (int) method.Invoke(null, new [] {val});
      }
      catch (Exception ex)
      {
        ourLogger.Verbose("Exception on evaluating PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup)"+ ex);
      }

      try
      {
        var property = typeof(PlayerSettings).GetProperty("apiCompatibilityLevel");
        apiCompatibilityLevel = (int) property.GetValue(null, null);
      }
      catch (Exception)
      {
        ourLogger.Verbose("Exception on evaluating PlayerSettings.apiCompatibilityLevel");
      }

      // Unity 5.5+ supports C# 6, but only when targeting .NET 4.6. The enum doesn't exist pre Unity 5.5
      const int apiCompatibilityLevelNet46 = 3;
      if (apiCompatibilityLevel >= apiCompatibilityLevelNet46)
        return "6";

      return "4";
    }

    private static void SetProjectFlavour(XElement projectElement, XNamespace xmlns)
    {
      // This is the VSTU project flavour GUID, followed by the C# project type
      SetOrUpdateProperty(projectElement, xmlns, "ProjectTypeGuids",
        "{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
    }

    private static void SetOrUpdateProperty(XElement root, XNamespace xmlns, string name, string content)
    {
      SetOrUpdateProperty(root, xmlns, name, v => content);
    }

    private static void SetOrUpdateProperty(XElement root, XNamespace xmlns, string name, Func<string, string> updater)
    {
      var element = root.Elements(xmlns + "PropertyGroup").Elements(xmlns + name).FirstOrDefault();
      if (element != null)
      {
        var result = updater(element.Value);
        if (result != element.Value)
        {
          ourLogger.Verbose("Overriding existing project property {0}. Old value: {1}, new value: {2}", name, element.Value, result);

          element.SetValue(result);
        }
        else
          ourLogger.Verbose("Property {0} already set. Old value: {1}, new value: {2}", name, element.Value, result);
      }
      else
        AddProperty(root, xmlns, name, updater(string.Empty));
    }

    // Adds a property to the first property group without a condition
    private static void AddProperty(XElement root, XNamespace xmlns, string name, string content)
    {
      ourLogger.Verbose("Adding project property {0}. Value: {1}", name, content);

      var propertyGroup = root.Elements(xmlns + "PropertyGroup")
        .FirstOrDefault(e => !e.Attributes(xmlns + "Condition").Any());
      if (propertyGroup == null)
      {
        propertyGroup = new XElement(xmlns + "PropertyGroup");
        root.AddFirst(propertyGroup);
      }

      propertyGroup.Add(new XElement(xmlns + name, content));
    }
  }
}