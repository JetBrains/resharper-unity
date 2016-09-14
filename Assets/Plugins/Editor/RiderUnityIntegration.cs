using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Put the file to Assets/Plugins/Rider/Editor/ for Unity 5.2.2+
// the file to Assets/Plugins/Editor/Rider for Unity prior 5.2.2

namespace Assets.Plugins.Editor
{
  [InitializeOnLoad]
  public static class Rider
  {
    private static readonly string SlnFile;
    private static readonly string DefaultApp = EditorPrefs.GetString("kScriptsDefaultApp");
    private static readonly FileInfo RiderFileInfo = new FileInfo(DefaultApp);
    internal static bool Enabled
    {
        get
        {
          if (string.IsNullOrEmpty(DefaultApp))
            return false;
          return RiderFileInfo.FullName.ToLower().Contains("rider") &&
            (RiderFileInfo.Exists || RiderFileInfo.Extension == ".app"); // seems like app doesn't exist as file
        }
    }

    static Rider()
    {
      if (Enabled)
      {
        var newPath = RiderFileInfo.FullName;
        // try to search the new version

        switch (RiderFileInfo.Extension)
        {
          /*
              Unity itself transforms lnk to exe
              case ".lnk":
              {
                if (riderFileInfo.Directory != null && riderFileInfo.Directory.Exists)
                {
                  var possibleNew = riderFileInfo.Directory.GetFiles("*ider*.lnk");
                  if (possibleNew.Length > 0)
                    newPath = possibleNew.OrderBy(a => a.LastWriteTime).Last().FullName;
                }
                break;
              }*/
          case ".exe":
          {
            var possibleNew =
              RiderFileInfo.Directory.Parent.Parent.GetDirectories("*ider*")
                .SelectMany(a => a.GetDirectories("bin")).SelectMany(a => a.GetFiles(RiderFileInfo.Name))
                .ToArray();
            if (possibleNew.Length > 0)
              newPath = possibleNew.OrderBy(a => a.LastWriteTime).Last().FullName;
            break;
          }
          case ".app": //osx
          {
            break;
          }
          default:
          {
            Debug.Log("Please manually update the path to Rider in Unity Preferences -> External Tools -> External Script Editor.");
            break;
          }
        }
        if (newPath != RiderFileInfo.FullName)
        {
          Debug.Log(RiderFileInfo.FullName);
          Debug.Log(newPath);
          EditorPrefs.SetString("kScriptsDefaultApp", newPath);
        }
      }

      // Open the solution file
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.Combine(projectDirectory, string.Format("{0}.sln", projectName));
    }

    /// <summary>
    /// Asset Open Callback (from Unity)
    /// </summary>
    /// <remarks>
    /// Called when Unity is about to open an asset.
    /// </remarks>
    [UnityEditor.Callbacks.OnOpenAssetAttribute()]
    static bool OnOpenedAsset(int instanceID, int line)
    {
      if (Enabled)
      {
        string appPath = Path.GetDirectoryName(Application.dataPath);

        // determine asset that has been double clicked in the project view
        var selected = EditorUtility.InstanceIDToObject(instanceID);

        if (selected.GetType().ToString() == "UnityEditor.MonoScript" ||
            selected.GetType().ToString() == "UnityEngine.Shader")
        {
          var completeFilepath = appPath + Path.DirectorySeparatorChar + AssetDatabase.GetAssetPath(selected);
          var args = string.Empty;
          if (GetPossibleRiderProcess() != null)
            args = string.Format(" -l {2} {0}{3}{0}", "\"", SlnFile, line, completeFilepath);
          else
            args = string.Format("{0}{1}{0} -l {2} {0}{3}{0}", "\"", SlnFile, line, completeFilepath);

          CallRider(RiderFileInfo.FullName, args);
          return true;
        }
      }
      return false;
    }

    private static void CallRider(string riderPath, string args)
    {
      var proc = new Process();
      if (new FileInfo(riderPath).Extension == ".app")
      {
        proc.StartInfo.FileName = "open";
        proc.StartInfo.Arguments = string.Format("-n {0}{1}{0} --args {2}", "\"", "/" + riderPath, args);
        Debug.Log(proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
      }
      else
      {
        proc.StartInfo.FileName = riderPath;
        proc.StartInfo.Arguments = args;
        Debug.Log("\"" + proc.StartInfo.FileName + "\"" + " " + proc.StartInfo.Arguments);
      }

      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
      proc.StartInfo.CreateNoWindow = true;
      proc.StartInfo.RedirectStandardOutput = true;
      proc.Start();

      if (new FileInfo(riderPath).Extension == ".exe")
      {
        try
        {
          ActivateWindow();
        }
        catch (Exception e)
        {
          Debug.Log("Exception on ActivateWindow: " + e);
        }
      }
    }

    private static void ActivateWindow()
    {
      var process = Process.GetProcesses().FirstOrDefault(b => !b.HasExited && b.ProcessName.Contains("Rider"));
      if (process != null)
      {
        // Collect top level windows
        var topLevelWindows = User32Dll.GetTopLevelWindowHandles();
        // Get process main window title
        var windowHandle = topLevelWindows.FirstOrDefault(hwnd => User32Dll.GetWindowProcessId(hwnd) == process.Id);
        if (windowHandle != IntPtr.Zero)
          User32Dll.SetForegroundWindow(windowHandle);
      }
    }

    private static Process GetPossibleRiderProcess()
    {
      var riderProcesses = Process.GetProcesses();
      foreach (var riderProcess in riderProcesses)
      {
        try
        {
          if (riderProcess.ProcessName.ToLower().Contains("rider"))
            return riderProcess;
        }
        catch (Exception)
        {
        }
      }
      return null;
    }

      [MenuItem("Assets/Open C# Project In Rider", false, 1000)]
      static void MenuOpenProject()
      {
          // Force the project files to be sync
          SyncSolution();

          // Load Project
          CallRider(RiderFileInfo.FullName, "\"" + SlnFile + "\"");
      }

      [MenuItem("Assets/Open C# Project In Rider", true, 1000)]
      static bool ValidateMenuOpenProject()
      {
          return Enabled;
      }

      /// <summary>
      /// Force Unity To Write Project File
      /// </summary>
      /// <remarks>
      /// Reflection!
      /// </remarks>
      public static void SyncSolution()
      {
          System.Type T = System.Type.GetType("UnityEditor.SyncVS,UnityEditor");
          System.Reflection.MethodInfo SyncSolution = T.GetMethod("SyncSolution",
              System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
          SyncSolution.Invoke(null, null);

      }
  }

  public static class User32Dll
  {

    /// <summary>
    /// Gets the ID of the process that owns the window.
    /// Note that creating a <see cref="Process"/> wrapper for that is very expensive because it causes an enumeration of all the system processes to happen.
    /// </summary>
    public static int GetWindowProcessId(IntPtr hwnd)
    {
      uint dwProcessId;
      GetWindowThreadProcessId(hwnd, out dwProcessId);
      return unchecked((int) dwProcessId);
    }


    /// <summary>
    /// Lists the handles of all the top-level windows currently available in the system.
    /// </summary>
    public static List<IntPtr> GetTopLevelWindowHandles()
    {
      var retval = new List<IntPtr>();
      EnumWindowsProc callback = (hwnd, param) =>
      {
        retval.Add(hwnd);
        return 1;
      };
      EnumWindows(Marshal.GetFunctionPointerForDelegate(callback), IntPtr.Zero);
      GC.KeepAlive(callback);
      return retval;
    }

    public delegate Int32 EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
    public static extern Int32 EnumWindows(IntPtr lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
    public static extern Int32 SetForegroundWindow(IntPtr hWnd);
  }

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

      Debug.Log(isModified ? "Project was post processed successfully" : "No change necessary in project");

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
  }
}

// Developed using JetBrains Rider =)