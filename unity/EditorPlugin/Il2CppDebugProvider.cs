#if UNITY_2019_2_OR_NEWER
using System;
using System.IO;
using System.Xml.Linq;
using JetBrains.Annotations;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class Il2CppDebugProvider
  {
    private const string UnityEngineDllsRole = "ManagedEngineAPI";
    private const string PlayerDllsRole = "ManagedLibrary";
    private static readonly string[] ourUnityEngineFallbackAssemblyNames = { "UnityEngine", "UnityEngine.CoreModule" };
    private static readonly string[] ourPlayerFallbackAssemblyNames = { "Assembly-CSharp" };

    public static string GenerateAdditionalLinkXmlFile([CanBeNull] BuildReport report, object data,
      bool preserveUnityEngineDlls, bool preservePlayerDlls)
    {
      //create a file in the random folder in the TEMP directory
      var filePath = Path.Combine(CreateRandomFolderInTempDirectory(), "linker.xml");

      //Fill temporary link.xml file with assembly names from BuildReport
      CreateLinkXmlFile(report, filePath, preserveUnityEngineDlls, preservePlayerDlls);

      return filePath;
    }

    private static string CreateRandomFolderInTempDirectory()
    {
      // Get the path of the Temp directory
      var tempPath = Path.GetTempPath();

      // Generate a random folder name
      var randomFolderName = Path.GetRandomFileName();

      // Combine the Temp path with the random folder name
      var randomFolderPath = Path.Combine(tempPath, randomFolderName);

      // Create the random folder
      Directory.CreateDirectory(randomFolderPath);

      return randomFolderPath;
    }

    private static void CreateLinkXmlFile([CanBeNull] BuildReport report, string filePath, bool preserveUnityEngineDlls,
      bool preservePlayerDlls)
    {
      // Create the root element
      var linker = new XElement("linker");

      if (preserveUnityEngineDlls)
        PreserveDlls(report, linker, UnityEngineDllsRole, ourUnityEngineFallbackAssemblyNames);

      if (preservePlayerDlls)
        PreserveDlls(report, linker, PlayerDllsRole, ourPlayerFallbackAssemblyNames);

      // Create the XML document
      var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), linker);

      doc.Save(filePath);
    }

    private static void PreserveDlls([CanBeNull] BuildReport report, XElement linker, string roleIdentifier,
      string[] fallbackAssemblies = null)
    {
      linker.Add(new XComment($"Preserve '{roleIdentifier}' assemblies"));

      // In general, BuildReport should never be null.
      // However, due to a known bug in Unity 2021, it can occasionally be null.
      // Unity has acknowledged this issue and has promised a fix in a future update.
      if (report == null)
      {
        if (fallbackAssemblies == null)
          return;

        foreach (var assembly in fallbackAssemblies)
          PreserveAssembly(linker, assembly);

        return;
      }

      foreach (var buildFile in GetReportFiles(report))
      {
        if (!buildFile.role.Equals(roleIdentifier))
          continue;

        var assemblyName = GetAssemblyName(buildFile);
        PreserveAssembly(linker, assemblyName);
      }
    }

    private static BuildFile[] GetReportFiles(BuildReport report)
    {
      // In Unity 2019, BuildReport.files exists and can be used without issues.
      // However, in Unity 2020 and higher, using BuildReport.files throws an exception.
      // For Unity 2020 and later versions, use BuildReport.GetFiles() instead.
      var type = typeof(BuildReport);
      try
      {
        var filesProperty = type.GetProperty("files");
        if (filesProperty != null)
        {
          var value = filesProperty.GetValue(report);
          if (value != null)
            return (BuildFile[])value;
        }
      }
      catch 
      {
        // Exception ignored - use BuildReport.GetFiles() as a fallback method.
      }

      try
      {
        var getFilesMethod = type.GetMethod("GetFiles");
        if (getFilesMethod != null)
        {
          var value = getFilesMethod.Invoke(report, Array.Empty<object>());
          if (value != null)
            return (BuildFile[])value;
        }
      }
      catch
      {
        // ignored
      }

      return Array.Empty<BuildFile>();
    }

    private static string GetAssemblyName(BuildFile buildFile)
    {
      return Path.GetFileNameWithoutExtension(buildFile.path);
    }

    private static void PreserveAssembly(XElement linker, string assemblyName)
    {
      // Create the assembly elements
      var assemblyElement = new XElement("assembly",
        new XAttribute("fullname", assemblyName),
        new XAttribute("preserve", "all")
      );

      linker.Add(assemblyElement);
    }
  }
}
#endif