using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.NonUnity;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public class CsprojAssetPostprocessor : AssetPostprocessor
  {
    private static readonly ILog ourLogger = Log.GetLog<CsprojAssetPostprocessor>();
    private const string UnityUnsafeKeyword = "-unsafe";
    private const string UnityDefineKeyword = "-define:";
    private const string UnityReferenceKeyword = "-r:";
    private static readonly string ourProjectManualConfigRoslynFilePath = Path.GetFullPath("Assets/csc.rsp");
    private static readonly string ourProjectManualConfigFilePath = Path.GetFullPath("Assets/mcs.rsp");
    private static readonly string ourPlayerProjectManualConfigFilePath = Path.GetFullPath("Assets/smcs.rsp");
    private static readonly string ourEditorProjectManualConfigFilePath = Path.GetFullPath("Assets/gmcs.rsp");
    private static int? ourApiCompatibilityLevel;
    private static int OurApiCompatibilityLevel
    {
      get
      {
        if (ourApiCompatibilityLevel == null) 
          ourApiCompatibilityLevel = GetApiCompatibilityLevel();
        return (int) ourApiCompatibilityLevel;
      }
    }
    private const int APICompatibilityLevelNet20Subset = 2;
    private const int APICompatibilityLevelNet46 = 3;

    // Note that this does not affect the order in which postprocessors are evaluated. Order of execution is undefined.
    // https://github.com/Unity-Technologies/UnityCsReference/blob/2018.2/Editor/Mono/AssetPostprocessor.cs#L152
    public override int GetPostprocessOrder()
    {
      return 10;
    }

    // This method is new for 2018.1. It allows multiple processors to modify the contents of the generated .csproj in
    // memory, and Unity will only write to disk if it's different to the existing file. It's safe for pre-2018.1 as it
    // simply won't get called https://github.com/Unity-Technologies/UnityCsReference/blob/2018.1/Editor/Mono/AssetPostprocessor.cs#L76
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public static string OnGeneratedCSProject(string path, string contents)
    {
      if (UnityUtils.IsInBatchModeAndNotInRiderTests)
        return contents;
      
      try
      {
        ourLogger.Verbose("Post-processing {0} (in memory)", path);
        var doc = XDocument.Parse(contents);
        if (UpgradeProjectFile(path, doc))
        {
          ourLogger.Verbose("Post-processed with changes {0} (in memory)", path);
          using (var sw = new Utf8StringWriter())
          {
            doc.Save(sw);
            return sw.ToString(); // https://github.com/JetBrains/resharper-unity/issues/727
          }
        }

        ourLogger.Verbose("Post-processed with NO changes {0}", path);
        return contents;
      }
      catch (Exception e)
      {
        // unhandled exception kills editor
        Debug.LogError(e);
        return contents;
      }
    }

    // This method is for pre-2018.1, and is called after the file has been written to disk
    public static void OnGeneratedCSProjectFiles()
    {
      if (UnityUtils.IsInBatchModeAndNotInRiderTests)
        return;
      
      if (UnityUtils.UnityVersion >= new Version(2018, 1))
        return;

      try
      {
        ourLogger.Verbose("Post-processing {0} (old version)");
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
      var projectContentElement = doc.Root;
      XNamespace xmlns = projectContentElement.Name.NamespaceName; // do not use var

      var changed = FixTargetFrameworkVersion(projectContentElement, xmlns); // no need for new Unity
      changed |= FixUnityEngineReference(projectContentElement, xmlns); // no need for new Unity
      changed |= FixSystemXml(projectContentElement, xmlns); // hopefully not needed
      changed |= SetLangVersion(projectContentElement, xmlns); // reimplemented in package
      changed |= SetProjectFlavour(projectContentElement, xmlns); // hopefully not needed
      changed |= SetManuallyDefinedCompilerSettings(projectFile, projectContentElement, xmlns); // hopefully not needed
      changed |= TrySetHintPathsForSystemAssemblies(projectContentElement, xmlns); // no need for new Unity
      changed |= FixImplicitReferences(projectContentElement, xmlns); // no need for new Unity
      changed |= AvoidGetReferenceAssemblyPathsCall(projectContentElement, xmlns); // reimplemeted
      changed |= AddMicrosoftCSharpReference(projectContentElement, xmlns); // not needed
      changed |= SetXCodeDllReference("UnityEditor.iOS.Extensions.Xcode.dll", projectContentElement, xmlns);
      changed |= SetXCodeDllReference("UnityEditor.iOS.Extensions.Common.dll", projectContentElement, xmlns);
      changed |= SetDisableHandlePackageFileConflicts(projectContentElement, xmlns); // already exists
      changed |= SetGenerateTargetFrameworkAttribute(projectContentElement, xmlns); // no need
      
      return changed;
    }

    /* Since Unity 2018.1.5f1 it looks like this:
     <PropertyGroup>
           <NoConfig>true</NoConfig>
           <NoStdLib>true</NoStdLib>
           <AddAdditionalExplicitAssemblyReferences>false</AddAdditionalExplicitAssemblyReferences>
           <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>
           <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>
         </PropertyGroup>
    */
    // https://github.com/JetBrains/resharper-unity/issues/988
    private static bool FixImplicitReferences(XElement projectContentElement, XNamespace xmlns)
    {
      var changed = false;

      // For Unity 2017.x, we are adding tags and reference to mscorlib
      if (UnityUtils.UnityVersion.Major == 2017)
      {
        // appears in Unity 2018.1.0b10
        SetOrUpdateProperty(projectContentElement, xmlns, "NoConfig", existing => "true");
        SetOrUpdateProperty(projectContentElement, xmlns, "NoStdLib", existing => "true");
        SetOrUpdateProperty(projectContentElement, xmlns, "AddAdditionalExplicitAssemblyReferences",existing => "false");

        // Unity 2018.x+ itself adds mscorlib reference 
        var referenceName = "mscorlib.dll";
        var hintPath = GetHintPath(referenceName);
        AddCustomReference(referenceName, projectContentElement, xmlns, hintPath);
        changed = true;
      }

      if (UnityUtils.UnityVersion.Major == 2017 || UnityUtils.UnityVersion.Major == 2018)
      {
        // appears in Unity 2018.1.5f1
        changed |= SetOrUpdateProperty(projectContentElement, xmlns, "ImplicitlyExpandNETStandardFacades",existing => "false");
        changed |= SetOrUpdateProperty(projectContentElement, xmlns, "ImplicitlyExpandDesignTimeFacades",existing => "false");
      }

      return changed;
    }

    // Computer may not have specific TargetFramework, msbuild will resolve System from different TargetFramework
    // If we set HintPaths together with DisableHandlePackageFileConflicts we help msbuild to resolve libs from Unity installation
    // Unity 2018+ already have HintPaths by default
    private static bool TrySetHintPathsForSystemAssemblies(XElement projectContentElement, XNamespace xmlns)
    {
      var elementsToUpdate = projectContentElement
        .Elements(xmlns+"ItemGroup")
        .Elements(xmlns+"Reference")
        .Where(a => a.Attribute("Include") != null && a.Elements(xmlns + "HintPath").SingleOrDefault() == null)
        .ToArray();
      foreach (var element in elementsToUpdate)
      {
        var referenceName = element.Attribute("Include").Value + ".dll";
        var hintPath = GetHintPath(referenceName);
        AddCustomReference(referenceName, projectContentElement, xmlns, hintPath);
      }

      if (elementsToUpdate.Any())
      {
        elementsToUpdate.Remove();
        return true;
      }

      return false;
    }

    private static bool SetGenerateTargetFrameworkAttribute(XElement projectContentElement, XNamespace xmlns)
    {
      //https://youtrack.jetbrains.com/issue/RIDER-17390
      
      if (UnityUtils.ScriptingRuntime > 0)  
        return false;
      
      return SetOrUpdateProperty(projectContentElement, xmlns, "GenerateTargetFrameworkAttribute", existing => "false");
    }

    private static bool AddMicrosoftCSharpReference (XElement projectContentElement, XNamespace xmlns)
    {
      string referenceName = "Microsoft.CSharp.dll";
      
      if (UnityUtils.ScriptingRuntime == 0)
        return false;

      if (OurApiCompatibilityLevel != APICompatibilityLevelNet46)
        return false;
      
      var hintPath = GetHintPath(referenceName);
      AddCustomReference(referenceName, projectContentElement, xmlns, hintPath);
      return true;
    }
    
    private static bool AvoidGetReferenceAssemblyPathsCall(XElement projectContentElement, XNamespace xmlns)
    {
      // Starting with Unity 2017, dotnet target pack is not required
      if (UnityUtils.UnityVersion.Major < 2017)
        return false;
      
      // Set _TargetFrameworkDirectories and _FullFrameworkReferenceAssemblyPaths to something to avoid GetReferenceAssemblyPaths task being called
      return SetOrUpdateProperty(projectContentElement, xmlns, "_TargetFrameworkDirectories", 
               existing => string.IsNullOrEmpty(existing) ? "non_empty_path_generated_by_rider_editor_plugin" : existing)
             &&
             SetOrUpdateProperty(projectContentElement, xmlns, "_FullFrameworkReferenceAssemblyPaths",
               existing => string.IsNullOrEmpty(existing) ? "non_empty_path_generated_by_rider_editor_plugin" : existing);
    }

    private static bool SetDisableHandlePackageFileConflicts(XElement projectContentElement, XNamespace xmlns)
    {
      // https://developercommunity.visualstudio.com/content/problem/138986/1550-preview-2-breaks-scriptsharp-compilation.html
      // RIDER-18316 Rider fails to resolve mscorlib

      return SetOrUpdateProperty(projectContentElement, xmlns, "DisableHandlePackageFileConflicts", existing => "true");
    }

    private static bool FixSystemXml(XElement projectContentElement, XNamespace xmlns)
    {
      var el = projectContentElement
        .Elements(xmlns+"ItemGroup")
        .Elements(xmlns+"Reference")
        .FirstOrDefault(a => a.Attribute("Include") !=null && a.Attribute("Include").Value=="System.XML");
      if (el != null)
      {
        el.Attribute("Include").Value = "System.Xml";
        return true;
      }

      return false;
    }

    private static bool SetManuallyDefinedCompilerSettings(string projectFile, XElement projectContentElement, XNamespace xmlns)
    {
      var configPath = GetConfigPath(projectFile);
      return ApplyManualCompilerSettings(configPath, projectContentElement, xmlns);
    }

    [CanBeNull]
    private static string GetConfigPath(string projectFile)
    {
      // First choice - prefer csc.rsp if it exists
      if (File.Exists(ourProjectManualConfigRoslynFilePath))
        return ourProjectManualConfigRoslynFilePath;

      // Second choice - prefer mcs.rsp if it exists
      if (File.Exists(ourProjectManualConfigFilePath))
        return ourProjectManualConfigFilePath;

      var filename = Path.GetFileName(projectFile);
      if (filename == "Assembly-CSharp.csproj")
        return ourPlayerProjectManualConfigFilePath;
      if (filename == "Assembly-CSharp-Editor.csproj")
        return ourEditorProjectManualConfigFilePath;

      return null;
    }

    private static bool ApplyManualCompilerSettings([CanBeNull] string configFilePath, XElement projectContentElement, XNamespace xmlns)
    {
      if (string.IsNullOrEmpty(configFilePath) || !File.Exists(configFilePath)) 
        return false;
      
      var configText = File.ReadAllText(configFilePath);
      var isUnity20171OrLater = UnityUtils.UnityVersion >= new Version(2017, 1);

      var changed = false;
      
      // Unity sets AllowUnsafeBlocks in 2017.1+ depending on Player settings or asmdef
      // Strictly necessary to compile unsafe code
      // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/VisualStudioIntegration/SolutionSynchronizationSettings.cs#L119
      if (configText.Contains(UnityUnsafeKeyword) && !isUnity20171OrLater)
      {
        changed |= ApplyAllowUnsafeBlocks(projectContentElement, xmlns);
      }

      // Unity natively handles this in 2017.1+
      // https://github.com/Unity-Technologies/UnityCsReference/blob/33cbfe062d795667c39e16777230e790fcd4b28b/Editor/Mono/VisualStudioIntegration/SolutionSynchronizer.cs#L191
      // Also note that we don't support the short "-d" form. Neither does Unity
      if (configText.Contains(UnityDefineKeyword) && !isUnity20171OrLater)
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
          if (f.Contains(UnityDefineKeyword))
          {
            var defineEndPos = f.IndexOf(UnityDefineKeyword) + UnityDefineKeyword.Length;
            var definesSubString = f.Substring(defineEndPos, f.Length - defineEndPos);
            definesSubString = definesSubString.Replace(";", ",");
            definesList.AddRange(definesSubString.Split(','));
          }
        }

        changed |= ApplyCustomDefines(definesList.ToArray(), projectContentElement, xmlns);
      }

      // Note that this doesn't handle the long version "-reference:"
      if (configText.Contains(UnityReferenceKeyword))
      {
        changed |= ApplyManualCompilerSettingsReferences(projectContentElement, xmlns, configText);
      }

      return changed;
    }

    private static bool ApplyCustomDefines(string[] customDefines, XElement projectContentElement, XNamespace xmlns)
    {
      var definesString = string.Join(";", customDefines);

      var defineConstants = projectContentElement
        .Elements(xmlns+"PropertyGroup")
        .Elements(xmlns+"DefineConstants")
        .FirstOrDefault(definesConsts=> !string.IsNullOrEmpty(definesConsts.Value));

      defineConstants?.SetValue(defineConstants.Value + ";" + definesString);
      return true;
    }

    private static bool ApplyAllowUnsafeBlocks(XElement projectContentElement, XNamespace xmlns)
    {
      projectContentElement.AddFirst(
        new XElement(xmlns + "PropertyGroup", new XElement(xmlns + "AllowUnsafeBlocks", true)));
      return true;
    }

    private static bool SetXCodeDllReference(string name, XElement projectContentElement, XNamespace xmlns)
    {
      // C:\Program Files\Unity\Editor\Data\PlaybackEngines\iOSSupport\
      var unityAppBaseDataFolder = Path.GetFullPath(EditorApplication.applicationContentsPath);
      var folders = new List<string> { unityAppBaseDataFolder};
      // https://github.com/JetBrains/resharper-unity/issues/841
      // /Applications/Unity/Hub/Editor/2018.2.10f1/PlaybackEngines/iOSSupport/
      var directoryInfo = new FileInfo(EditorApplication.applicationPath).Directory;
      if (directoryInfo != null) 
        folders.Add(directoryInfo.FullName);

      var xcodeDllPath = folders
        .Select(folder => Path.Combine(folder, Path.Combine("PlaybackEngines/iOSSupport", name)))
        .Where(File.Exists).FirstOrDefault();
      
      if (string.IsNullOrEmpty(xcodeDllPath)) 
        return false;
      
      AddCustomReference(Path.GetFileNameWithoutExtension(xcodeDllPath), projectContentElement, xmlns, xcodeDllPath);
      return true;
    }

    private static bool FixUnityEngineReference(XElement projectContentElement, XNamespace xmlns)
    {
      // Handled natively by Unity 2018.2+
      if (UnityUtils.UnityVersion >= new Version(2018, 2))
        return false;
      
      var unityAppBaseFolder = Path.GetDirectoryName(EditorApplication.applicationPath);
      if (string.IsNullOrEmpty(unityAppBaseFolder))
      {
        ourLogger.Verbose("FixUnityEngineReference. unityAppBaseFolder IsNullOrEmpty");
        return false;
      }

      var el = projectContentElement
        .Elements(xmlns+"ItemGroup")
        .Elements(xmlns+"Reference")
        .FirstOrDefault(a => a.Attribute("Include") !=null && a.Attribute("Include").Value=="UnityEngine");
      var hintPath = el?.Elements(xmlns + "HintPath").FirstOrDefault();
      if (hintPath == null)
        return false;

      var oldUnityEngineDllFileInfo = new FileInfo(hintPath.Value);
      var unityEngineDir = new DirectoryInfo(Path.Combine(oldUnityEngineDllFileInfo.Directory.FullName, "UnityEngine"));
      if (!unityEngineDir.Exists)
        return false;

      var newDllPath = Path.Combine(unityEngineDir.FullName, "UnityEngine.dll");
      if (!File.Exists(newDllPath))
        return false;

      hintPath.SetValue(newDllPath);

      var files = unityEngineDir.GetFiles("*.dll");
      foreach (var file in files)
      {
        AddCustomReference(Path.GetFileNameWithoutExtension(file.Name), projectContentElement, xmlns, file.FullName);
      }

      return true;
    }

    private static bool ApplyManualCompilerSettingsReferences(XElement projectContentElement, XNamespace xmlns, string configText)
    {
      var referenceList = new List<string>();
      var compileFlags = configText.Split(' ', '\n');
      foreach (var flag in compileFlags)
      {
        var f = flag.Trim();
        if (f.Contains(UnityReferenceKeyword))
        {
          var defineEndPos = f.IndexOf(UnityReferenceKeyword) + UnityReferenceKeyword.Length;
          var definesSubString = f.Substring(defineEndPos, f.Length - defineEndPos);
          definesSubString = definesSubString.Replace(";", ",");
          referenceList.AddRange(definesSubString.Split(','));
        }
      }

      foreach (var reference in referenceList)
      {
        var name = reference.Trim().TrimStart('"').TrimEnd('"');
        var nameFileInfo = new FileInfo(name);
        if (nameFileInfo.Extension.ToLower() != ".dll")
          name += ".dll"; // RIDER-15093

        string hintPath;
        if (!nameFileInfo.Exists)
          hintPath = GetHintPath(name);
        else
          hintPath = nameFileInfo.FullName;
        AddCustomReference(name, projectContentElement, xmlns, hintPath);
      }

      return true;
    }

    [CanBeNull]
    private static string GetHintPath(string name)
    {
      // Without HintPath non-Unity MSBuild will resolve assembly from DotNetFramework targets path
      string hintPath = null;
      
      var unityAppBaseFolder = Path.GetFullPath(EditorApplication.applicationContentsPath);
      var monoDir = new DirectoryInfo(Path.Combine(unityAppBaseFolder, "MonoBleedingEdge/lib/mono"));
      if (!monoDir.Exists)
        monoDir = new DirectoryInfo(Path.Combine(unityAppBaseFolder, "Data/MonoBleedingEdge/lib/mono"));

      var mask = "4.*";
      if (UnityUtils.ScriptingRuntime == 0)
      {
        mask = "2.*"; // 1 = ApiCompatibilityLevel.NET_2_0
        if (OurApiCompatibilityLevel == APICompatibilityLevelNet20Subset) // ApiCompatibilityLevel.NET_2_0_Subset
          mask = "unity";
      }

      if (!monoDir.Exists)
        return null;
      
      var apiDir = monoDir.GetDirectories(mask).LastOrDefault(); // take newest
      if (apiDir != null)
      {
        var dllPath = new FileInfo(Path.Combine(apiDir.FullName, name));
        if (dllPath.Exists)
          hintPath = dllPath.FullName;
      }

      return hintPath;
    }

    private static void AddCustomReference(string name, XElement projectContentElement, XNamespace xmlns, string hintPath = null)
    {
      ourLogger.Verbose($"AddCustomReference {name}, {hintPath}");
      var itemGroup = projectContentElement.Elements(xmlns + "ItemGroup").FirstOrDefault();
      if (itemGroup == null)
      {
        ourLogger.Verbose("Skip AddCustomReference, ItemGroup is null.");
        return;
      }
      var reference = new XElement(xmlns + "Reference");
      reference.Add(new XAttribute("Include", Path.GetFileNameWithoutExtension(name)));
      if (!string.IsNullOrEmpty(hintPath))
        reference.Add(new XElement(xmlns + "HintPath", hintPath));
      itemGroup.Add(reference);
    }

    // Set appropriate version
    private static bool FixTargetFrameworkVersion(XElement projectElement, XNamespace xmlns)
    {
      return SetOrUpdateProperty(projectElement, xmlns, "TargetFrameworkVersion", s =>
        {
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
              return "v" + PluginSettings.TargetFrameworkVersionOldMono;
            }
          }

          if (string.IsNullOrEmpty(s))
          {
            ourLogger.Verbose("TargetFrameworkVersion in csproj is null or empty.");
            return string.Empty;
          }

          var version = string.Empty;
          try
          {
            version = s.Substring(1);
            // for windows try to use installed dotnet framework
            // Unity 2018.1 doesn't require installed dotnet framework, it references everything from Unity installation
            if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows && UnityUtils.UnityVersion < new Version(2018, 1))
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

          return "v" + version;
        }
      );
    }

    private static bool SetLangVersion(XElement projectElement, XNamespace xmlns)
    {
      // Set the C# language level, so Rider doesn't have to guess (although it does a good job)
      // VSTU sets this, and I think newer versions of Unity do too (should check which version)
      return SetOrUpdateProperty(projectElement, xmlns, "LangVersion", existing =>
      {
        if (PluginSettings.OverrideLangVersion)
        {
          return PluginSettings.LangVersion;
        }
        
        var expected = GetExpectedLanguageLevel();
        if (string.IsNullOrEmpty(existing))
          return expected;

        if (existing == "default")
          return expected;
        
        if (expected == "latest" || existing == "latest")
          return "latest";

        // Only use our version if it's not already set, or it's less than what we would set
        var currentIsParsed = VersionExtensions.TryParse(existing, out var currentLanguageLevel);
        var expectedIsParsed = VersionExtensions.TryParse(expected, out var expectedLanguageLevel);
        if (currentIsParsed && expectedIsParsed && currentLanguageLevel < expectedLanguageLevel)
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

      // Unity 5.5+ supports C# 6, but only when targeting .NET 4.6. The enum doesn't exist pre Unity 5.5
      if (OurApiCompatibilityLevel >= APICompatibilityLevelNet46)
        return "6";

      return "4";
    }

    private static int GetApiCompatibilityLevel()
    {
      var apiCompatibilityLevel = 0;
      try
      {
        //PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup)
        var method = typeof(PlayerSettings).GetMethod("GetApiCompatibilityLevel");
        var parameter = typeof(EditorUserBuildSettings).GetProperty("selectedBuildTargetGroup");
        var val = parameter.GetValue(null, null);
        apiCompatibilityLevel = (int) method.Invoke(null, new[] {val});
      }
      catch (Exception ex)
      {
        ourLogger.Verbose(
          "Exception on evaluating PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup)" +
          ex);
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

      return apiCompatibilityLevel;
    }

    private static bool SetProjectFlavour(XElement projectElement, XNamespace xmlns)
    {
      // This is the VSTU project flavour GUID, followed by the C# project type
      return SetOrUpdateProperty(projectElement, xmlns, "ProjectTypeGuids",
        "{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}");
    }

    private static bool SetOrUpdateProperty(XElement root, XNamespace xmlns, string name, string content)
    {
      return SetOrUpdateProperty(root, xmlns, name, v => content);
    }

    private static bool SetOrUpdateProperty(XElement root, XNamespace xmlns, string name, Func<string, string> updater)
    {
      var elements = root.Elements(xmlns + "PropertyGroup").Elements(xmlns + name).ToList();
      if (elements.Any())
      {
        var updated = false;
        foreach (var element in elements)
        {
          var result = updater(element.Value);
          if (result != element.Value)
          {
            ourLogger.Verbose("Overriding existing project property {0}. Old value: {1}, new value: {2}", name,
              element.Value, result);

            element.SetValue(result);
            updated = true;
          }
          ourLogger.Verbose("Property {0} already set. Old value: {1}, new value: {2}", name, element.Value, result);
        }

        return updated;
      }

      AddProperty(root, xmlns, name, updater(string.Empty));
      return true;
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

    class Utf8StringWriter : StringWriter
    {
      public override Encoding Encoding => Encoding.UTF8;
    }
  }
}