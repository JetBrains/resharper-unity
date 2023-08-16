using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace JetBrains.Rider.PathLocator
{
  public class RiderFileOpener
  {
    private readonly IRiderLocatorEnvironment myRiderLocatorEnvironment;

    public RiderFileOpener(IRiderLocatorEnvironment riderLocatorEnvironment)
    {
      myRiderLocatorEnvironment = riderLocatorEnvironment;
    }

    public static bool IsFleet(FileInfo appFileInfo)
    {
      return appFileInfo.Name.StartsWith("fleet", StringComparison.OrdinalIgnoreCase);
    }
    
    public bool OpenFile(string appPath, string slnFile, string assetFilePath, int line, int column)
    {
      // on mac empty string in quotes is causing additional solution to be opened https://github.cds.internal.unity3d.com/unity/com.unity.ide.rider/issues/21
      var pathArguments = assetFilePath == string.Empty ? string.Empty : $" --line {line} --column {column} \"{assetFilePath}\""; 
      var args = $"\"{slnFile}\"{pathArguments}";

      bool isFleet = IsFleet(new FileInfo(appPath));
      
      if (isFleet)
      {
        var pathArgumentsFleet = assetFilePath == string.Empty ? string.Empty : $"--goto=\"{assetFilePath}\"";
        if (line >= 0) // FL-20548 Fleet doesn't like -1:-1 in the goto
        {
          pathArgumentsFleet += $":{line}";
          if (column >= 0)
          {
            pathArgumentsFleet += $":{column}";
          }
        }
        
        var solutionDir = new FileInfo(slnFile).Directory.FullName;
        args = $"fleet://open?arg_0=\"{solutionDir}\"&arg_1={pathArgumentsFleet}";
      }

      var proc = new Process();
      if (myRiderLocatorEnvironment.CurrentOS == OS.MacOSX)
      {
        proc.StartInfo.FileName = "open";
        proc.StartInfo.Arguments = isFleet 
          ? $"-a \"{appPath}\" \"{args}\"" 
          : $"-n \"{appPath}\" --args {args}";
      }
      else
      {
        proc.StartInfo.FileName = appPath;
        proc.StartInfo.Arguments = args;
      }
      proc.StartInfo.UseShellExecute = true; // avoid HandleInheritance
      var message = $"\"{proc.StartInfo.FileName}\" {proc.StartInfo.Arguments}";
      myRiderLocatorEnvironment.Verbose(message);
      if (!proc.Start())
      {
        myRiderLocatorEnvironment.Error($"Process failed to start. {message}");
        return false;
      }
      AllowSetForegroundWindow(proc.Id);
      return true;
    }

    // This is required to be called to help focus itself
    public void AllowSetForegroundWindow(int? processId=null)
    {
      if (myRiderLocatorEnvironment.CurrentOS != OS.Windows)
        return;

      try
      {
        var process = processId == null ? GetRiderProcess() : Process.GetProcessById((int)processId);
        if (process == null)
          return;

        if (process.Id > 0)
          User32Dll.AllowSetForegroundWindow(process.Id);
      }
      catch (Exception e)
      {
        myRiderLocatorEnvironment.Warn("Exception on AllowSetForegroundWindow: " + e);
      }
    }

    private static Process GetRiderProcess()
    {
      var process = Process.GetProcesses().FirstOrDefault(p =>
      {
        string processName;
        try
        {
          processName =
            p.ProcessName; // some processes like kaspersky antivirus throw exception on attempt to get ProcessName
        }
        catch (Exception)
        {
          return false;
        }

        return !p.HasExited && processName.ToLower().Contains("rider");
      });
      return process;
    }
  }
}