using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;

namespace Assets.Plugins.Editor.Rider
{
  public class RiderAssetPostprocessor : AssetPostprocessor
  {
    public static void OnGeneratedCSProjectFiles()
    {
      if (!RiderPlugin.Enabled)
        return;
      var currentDirectory = Directory.GetCurrentDirectory();
      var projectFiles = Directory.GetFiles(currentDirectory, "*.csproj");

      foreach (var file in projectFiles)
      {
        UpgradeProjectFile(file);
      }
    }

    private static void UpgradeProjectFile(string projectFile)
    {
      RiderPlugin.Log(string.Format("Post-processing {0}", projectFile));
      var doc = XDocument.Load(projectFile);
      var projectContentElement = doc.Root;
      XNamespace xmlns = projectContentElement.Name.NamespaceName; // do not use var

      if (!RiderPlugin.IsDotNetFrameworkUsed)
      {
        // helps resolve System.Linq under mono 4
        var xNodes = projectContentElement.Elements().ToList();
        var targetFrameworkVersion =
          xNodes.Elements().FirstOrDefault(childNode => childNode.Name.LocalName == "TargetFrameworkVersion");
        targetFrameworkVersion.SetValue("v4.5");
      }

      if (Environment.Version.Major < 4 && !CSharp60Support())
      {
        // C# 6 is not supported
        var group = projectContentElement.Elements().FirstOrDefault(childNode => childNode.Name.LocalName == "PropertyGroup");
        var lang = group.Elements("LangVersion").FirstOrDefault();
        if (lang != null)
        {
          lang.SetValue("5");
        }
        else
        {
          var newLang = new XElement(xmlns + "LangVersion");
          newLang.SetValue("5");
          group.Add(newLang);
        }
      }

      SetXCodeDllReference("UnityEditor.iOS.Extensions.Xcode.dll", xmlns, projectContentElement);
      SetXCodeDllReference("UnityEditor.iOS.Extensions.Common.dll", xmlns, projectContentElement);

      doc.Save(projectFile);
    }

    private static void SetXCodeDllReference(string name, XNamespace xmlns, XElement projectContentElement)
    {
      var xcodeDllPath = Path.Combine("C:/Program Files/Unity/Editor/Data/PlaybackEngines/iOSSupport", name);
      if (!File.Exists(xcodeDllPath))
        xcodeDllPath = Path.Combine("/Applications/Unity/PlaybackEngines/iOSSupport", name);
      if (File.Exists(xcodeDllPath))
      {
        var itemGroup = new XElement(xmlns + "ItemGroup");
        var reference = new XElement(xmlns + "Reference");
        reference.Add(new XAttribute("Include", Path.GetFileNameWithoutExtension(xcodeDllPath)));
        reference.Add(new XElement(xmlns + "HintPath", xcodeDllPath));
        itemGroup.Add(reference);
        projectContentElement.Add(itemGroup);
      }
    }

    private static bool CSharp60Support()
    {
      bool res = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetExportedTypes())
        .Any(type => type.Name == "UnitySynchronizationContext");
      return res;
    }
  }
}