using System;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public static class RiderPackageInterop
  {
    private static readonly ILog ourLogger = Log.GetLog("RiderPackageInterop");
    
    public static Assembly GetAssembly()
    {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      var riderPackageAssembly = assemblies
        .FirstOrDefault(assembly => assembly.GetName().Name.Equals("Unity.Rider.Editor"));
      if (riderPackageAssembly == null)
      {
        ourLogger.Verbose("Could not find Unity.Rider.Editor assembly in current AppDomain");
      }

      return riderPackageAssembly;
    }
  }
}