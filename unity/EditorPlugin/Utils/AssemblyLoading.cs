using System;
using System.Collections.Generic;
using System.Reflection;

namespace JetBrains.Rider.Unity.Editor.Utils
{
  // starting with 6.5 unity started reworking assembly loading to better support hot-reload and future coreclr,
  // the old apis are not recommended anymore and can lead to problems, and we have to use the new ones, if they are
  // available, so we store a number of relevant API delegates here and select them based on the unity version
  public static class AssemblyLoading
  {
    public delegate string GetLoadedAssemblyPathDelegate(Assembly assembly);
    public static GetLoadedAssemblyPathDelegate GetLoadedAssemblyPath;

    public delegate Assembly LoadFromPathDelegate(string assemblyPath);
    public static LoadFromPathDelegate LoadFromPath;

    private delegate IReadOnlyList<Assembly> GetAssembliesDelegate();
    private static readonly GetAssembliesDelegate ourGetAssemblies;

    public static Assembly GetAssembly(string name)
    {
      foreach (var assembly in ourGetAssemblies())
      {
        if (assembly.GetName().Name.Equals(name))
          return assembly;
      }
      return null;
    }
    
    static AssemblyLoading()
    {
      if (UnityUtils.UnityVersion >= new Version(6000, 5))
      {
        var currentAssembliesType = Type.GetType("UnityEngine.Assemblies.CurrentAssemblies, UnityEngine.CoreModule");
        var getLoadedAssembliesMethod = currentAssembliesType.GetMethod("GetLoadedAssemblies", BindingFlags.Public | BindingFlags.Static);
        ourGetAssemblies = (GetAssembliesDelegate)getLoadedAssembliesMethod.CreateDelegate(typeof(GetAssembliesDelegate));

        var loadFromPathMethod = currentAssembliesType.GetMethod("LoadFromPath", BindingFlags.Public | BindingFlags.Static);
        LoadFromPath = (LoadFromPathDelegate)loadFromPathMethod.CreateDelegate(typeof(LoadFromPathDelegate));

        var assemblyExtensionType = Type.GetType("UnityEngine.AssemblyExtension, UnityEngine.CoreModule");
        var getLoadedAssemblyPathMethod = assemblyExtensionType.GetMethod("GetLoadedAssemblyPath", BindingFlags.Public | BindingFlags.Static);
        GetLoadedAssemblyPath = (GetLoadedAssemblyPathDelegate)getLoadedAssemblyPathMethod.CreateDelegate(typeof(GetLoadedAssemblyPathDelegate));
      }
      else
      {
        ourGetAssemblies = AppDomain.CurrentDomain.GetAssemblies;
        GetLoadedAssemblyPath = static assembly => assembly.Location;
        LoadFromPath = static path => AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path));
      }
    }
  }
}