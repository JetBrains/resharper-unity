using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;

namespace Assets.Plugins.Editor.Rider
{
  public class RiderAssetPostprocessor : AssetPostprocessor
  {
    public static void OnGeneratedCSProjectFiles()
    {
      if (!Rider.Enabled)
        return;
      var currentDirectory = Directory.GetCurrentDirectory();
      var projectFiles = Directory.GetFiles(currentDirectory, "*.csproj");

      bool isModified = false;
      foreach (var file in projectFiles)
      {
        isModified = UpgradeProjectFile(file);
      }

      var slnFiles = Directory.GetFiles(currentDirectory, "*.sln"); // piece from MLTimK fork
      foreach (var file in slnFiles)
      {
        string content = File.ReadAllText(file);
        const string magicProjectGUID = @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"")";
        // guid representing C# project
        if (!content.Contains(magicProjectGUID))
        {
          string matchGUID = @"Project\(\""\{[A-Z0-9]{8}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{12}\}\""\)";
          // Unity may put a random guid, which will brake Rider goto
          content = Regex.Replace(content, matchGUID, magicProjectGUID);
          File.WriteAllText(file, content);
          isModified = true;
        }
      }

      Rider.Log(isModified ? "Solution was post processed successfully" : "No change necessary");
      UpdateUnitySettings(slnFiles);
      UpdateDebugSettings();
    }

    private static bool UpgradeProjectFile(string projectFile)
    {
      var doc = XDocument.Load(projectFile);
      var projectContentElement = doc.Root;
      XNamespace xmlns = projectContentElement.Name.NamespaceName; // do not use var

      if (!Rider.IsDotNetFrameworkUsed)
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

      doc.Save(projectFile);
      return true;
    }

    private static bool CSharp60Support()
    {
      bool res = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(assembly => assembly.GetExportedTypes())
        .Any(type => type.Name == "UnitySynchronizationContext");
      return res;
    }

    private static void UpdateUnitySettings(string[] slnFiles)
    {
      try
      {
        if (slnFiles.Any())
          EditorPrefs.SetString("kScriptEditorArgs", "\"" + slnFiles.First() + "\"");
        else
          EditorPrefs.SetString("kScriptEditorArgs", string.Empty);
      }
      catch (Exception e)
      {
        Rider.Log("Exception on updating kScriptEditorArgs: " + e.Message);
      }
    }

    // initial version copied from https://github.com/yonstorm/ProjectRider-Unity/blob/develop/Assets/Plugins/Editor/ProjectRider/ProjectValidator.cs
    private static bool UpdateDebugSettings()
    {
      var workspaceFile =
        Path.Combine(
          Path.Combine(
            Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), ".idea"),
              ".idea." + Path.GetFileNameWithoutExtension(Rider.SlnFile)), ".idea"), "workspace.xml");
      if (!File.Exists(workspaceFile))
      {
        // TODO: write workspace settings from a template to be able to write debug settings before Rider is started for the first time.
        Rider.Log(workspaceFile + " doesn't exist.");
        return true;
      }

      var document = XDocument.Load(workspaceFile);
      var runManagerElement = (from elem in document.Descendants()
        where elem.Attribute("name") != null && elem.Attribute("name").Value.Equals("RunManager")
        select elem).FirstOrDefault();

      if (runManagerElement == null)
      {
        var projectElement = document.Element("project");
        if (projectElement == null)
          return false;

        runManagerElement = new XElement("component", new XAttribute("name", "RunManager"));
        projectElement.Add(runManagerElement);
      }

      var editorConfigElem = (from elem in runManagerElement.Descendants()
        where elem.Attribute("name") != null && elem.Attribute("name").Value.Equals("UnityEditor-generated")
        select elem).FirstOrDefault();

      var currentDebugPort = GetDebugPort();
      if (editorConfigElem == null)
      {
        editorConfigElem = new XElement("configuration");
        var defaultAttr = new XAttribute("default", false);
        var nameAttr = new XAttribute("name", "UnityEditor-generated");
        var typeAttr = new XAttribute("type", "ConnectRemote");
        var factoryNameAttr = new XAttribute("factoryName", "Mono remote");
        var showStdErrAttr = new XAttribute("show_console_on_std_err", false);
        var showStdOutAttr = new XAttribute("show_console_on_std_out", true);

        editorConfigElem.Add(defaultAttr, nameAttr, typeAttr, factoryNameAttr, showStdErrAttr, showStdOutAttr);

        var optionAdress = new XElement("option");
        optionAdress.Add(new XAttribute("name", "address"), new XAttribute("value", "localhost"));
        var optionPort = new XElement("option");
        optionPort.Add(new XAttribute("name", "port"), new XAttribute("value", currentDebugPort.ToString()));

        editorConfigElem.Add(optionAdress, optionPort);

        runManagerElement.SetAttributeValue("selected", "Mono remote.UnityEditor-generated");
        runManagerElement.Add(editorConfigElem);
      }
      else
      {
        var el = editorConfigElem.Descendants("option").Single(a => a.Attribute("name").Value == "port");
        el.Attribute("value").SetValue(currentDebugPort.ToString());
      }

      document.Save(workspaceFile);

      // Rider doesn't like it small... :/
      var lines = File.ReadAllLines(workspaceFile);
      lines[0] = lines[0].Replace("utf-8", "UTF-8");
      File.WriteAllLines(workspaceFile, lines);

      return true;
    }

    private static int GetDebugPort()
    {
      var processId = Process.GetCurrentProcess().Id;
      var port = 56000 + (processId % 1000);

      return port;
    }
  }
}