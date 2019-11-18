using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public class PdbAssetPostprocessor : AssetPostprocessor
  {
    private static readonly ILog ourLogger = Log.GetLog<PdbAssetPostprocessor>();
    
    public override int GetPostprocessOrder()
    {
      return 10;
    }
    
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
    {
      if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
        return;
      
      try
      {
        var toBeConverted = importedAssets.Where(a => 
            a.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
            importedAssets.Any(a1 => a1 == Path.ChangeExtension(a, ".pdb")) &&
            importedAssets.All(b => b != Path.ChangeExtension(a, ".dll.mdb")))
          .ToArray();
        foreach (var asset in toBeConverted)
        {
          var dllPath = Path.GetFullPath(asset);
          if (!AssemblyIsInAppDomain(dllPath)) continue;
          var pdb = Path.ChangeExtension(dllPath, ".pdb");
          if (!IsPortablePdb(pdb))
          {
            ConvertSymbolsForAssembly(dllPath);
            var mdbFile = Path.ChangeExtension(dllPath, ".dll.mdb");
            if (new FileInfo(mdbFile).Exists)
              AssetDatabase.ImportAsset(mdbFile);
          }
          else
            ourLogger.Verbose("mdb generation for Portable pdb is not supported. {0}", pdb);
        }
      }
      catch (Exception e)
      {
        // unhandled exception kills editor
        Debug.LogError(e);
      }
    }

    private static bool AssemblyIsInAppDomain(string dllPath)
    {
      // managed dll is present here
      return AppDomain.CurrentDomain.GetAssemblies().Any(a =>
      {
        var result = false;
        try
        {
          result = a.Location == dllPath; // dynamic modules throw on asking Location
        }
        catch { 
          // ignored
        }
        return result;
      });
    }

    private static Type ourPdb2MdbDriver;
    private static Type Pdb2MdbDriver
    {
      get
      {
        if (ourPdb2MdbDriver != null)
          return ourPdb2MdbDriver;
        Assembly assembly;
        try
        {
          var ass = Assembly.GetExecutingAssembly();
          ourLogger.Verbose("resources in {0}: {1}", ass, ass.GetManifestResourceNames().Aggregate((a,b)=>a+", "+b));
          
          const string resourceName = "JetBrains.Rider.Unity.Editor.pdb2mdb.exe";
          using (var resourceStream = ass.GetManifestResourceStream(resourceName))
          {
            using (var memoryStream = new MemoryStream())
            {
              if (resourceStream == null)
              {
                ourLogger.Error("Plugin file not found in manifest resources. " + resourceName);
                return null;
              }
              CopyStream(resourceStream, memoryStream);
              assembly = Assembly.Load(memoryStream.ToArray());
            }    
          }
        }
        catch (Exception)
        {
          ourLogger.Verbose("Loading pdb2mdb failed.");
          assembly = null;
        }

        if (assembly == null)
          return null;
        var type = assembly.GetType("Pdb2Mdb.Driver");
        if (type == null)
          return null;
        return ourPdb2MdbDriver = type;
      }
    }
    
    public static void CopyStream(Stream origin, Stream target)
    {
      var buffer = new byte[8192];
      int count;
      while ((count = origin.Read(buffer, 0, buffer.Length)) > 0)
        target.Write(buffer, 0, count);
    }

    private static void ConvertSymbolsForAssembly(string dllPath)
    {
      if (Pdb2MdbDriver == null)
      {
        ourLogger.Warn("FailedToConvertDebugSymbolsNoPdb2mdb.");
        return;
      }
      
      var method = Pdb2MdbDriver.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
      if (method == null)
      {
        ourLogger.Warn("WarningFailedToConvertDebugSymbolsPdb2mdbMainIsNull.");
        return;
      }

      var strArray = new[] { dllPath };
      method.Invoke(null, new object[] { strArray });
    }
    
    //https://github.com/xamarin/xamarin-android/commit/4e30546f
    private const uint PpdbSignature = 0x424a5342;
    public static bool IsPortablePdb(string filename)
    {
      try
      {
        using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
          using (var br = new BinaryReader(fs))
          {
            return br.ReadUInt32() == PpdbSignature;
          }
        }
      }
      catch
      {
        return false;
      }
    }
  }
}