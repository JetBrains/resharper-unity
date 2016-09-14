using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

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
                string content = File.ReadAllText(file);
                if (content.Contains("<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>"))
                {
                    content = Regex.Replace(content, "<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>",
                        "<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>");
                    File.WriteAllText(file, content);
                    isModified = true;
                }

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

            Debug.Log(isModified ? "[Rider] Project was post processed successfully" : "[Rider] No change necessary in project");

            UpdateUnitySettings(slnFiles);
            UpdateDotSettings();
            UpdateDebugSettings();
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
                Debug.Log("Exception on updating kScriptEditorArgs: " + e.Message);
            }
        }

        // copied from https://github.com/yonstorm/ProjectRider-Unity/blob/develop/Assets/Plugins/Editor/ProjectRider/ProjectValidator.cs
        private static bool UpdateDotSettings()
        {
            Debug.Log("[Rider] Updating... dot settings");
            var projectFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");

            foreach (var file in projectFiles)
            {
                var dotSettingsFile = file + ".DotSettings";

                if (File.Exists(dotSettingsFile))
                {
                    continue;
                }

                CreateDotSettingsFile(dotSettingsFile, DotSettingsContent);
            }

            return true;
        }

        private static bool UpdateDebugSettings()
        {
            Debug.Log("[Rider] Updating... debug settings");
            var workspaceFile = Path.Combine(Path.Combine(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), ".idea"),".idea."+Path.GetFileNameWithoutExtension(Rider.SlnFile)),".idea"), "workspace.xml");
            if (!File.Exists(workspaceFile))
            {
                // TODO: write workspace settings from a template to be able to write debug settings before Rider is started for the first time.
                //return true;
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
                var portAttr = new XAttribute("b", currentDebugPort);
                var addressAttr = new XAttribute("a", "127.0.0.1");

                editorConfigElem.Add(defaultAttr, nameAttr, typeAttr, factoryNameAttr, showStdErrAttr, showStdOutAttr,
                    portAttr, addressAttr);

                var optionAdress = new XElement("option");
                optionAdress.Add(new XAttribute("address", "127.0.0.1"));
                var optionPort = new XElement("option");
                optionPort.Add(new XAttribute("port", currentDebugPort.ToString()));

                editorConfigElem.Add(optionAdress, optionPort);

                runManagerElement.SetAttributeValue("selected", "Mono remote.UnityEditor-generated");
                runManagerElement.Add(editorConfigElem);
            }
            else
            {
                editorConfigElem.Attribute("b").Value = currentDebugPort.ToString();
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

        private static void CreateDotSettingsFile(string dotSettingsFile, string content)
        {
            using (var writer = File.CreateText(dotSettingsFile))
            {
                writer.Write(content);
            }
        }

        private static int GetDebugPort()
        {
            var processId = Process.GetCurrentProcess().Id;
            var port = 56000 + (processId % 1000);

            return port;
        }

        private const string DotSettingsContent =
            @"<wpf:ResourceDictionary xml:space=""preserve"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:s=""clr-namespace:System;assembly=mscorlib"" xmlns:ss=""urn:shemas-jetbrains-com:settings-storage-xaml"" xmlns:wpf=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                                                                    		<s:String x:Key=""/Default/CodeInspection/CSharpLanguageProject/LanguageLevel/@EntryValue"">CSharp50</s:String></wpf:ResourceDictionary>";

    }
}