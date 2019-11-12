using System;
using System.Reflection;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor
{
  public static class UIElementsSupport
  {
    private static readonly ILog ourLogger = Log.GetLog("UIElementsSupport");

    public static bool GenerateSchema()
    {
      // This type was first introduced in 2019.1
      var generator = Type.GetType("UnityEditor.UIElements.UxmlSchemaGenerator,UnityEditor");
      if (generator == null)
      {
        ourLogger.Verbose("Cannot find type: UnityEditor.UIElements.UxmlSchemaGenerator. Trying obsolete API");

        // The schema generator was first introduced in experimental namespace in 2018.2
        generator = Type.GetType("UnityEditor.Experimental.UIElements.UxmlSchemaGenerator,UnityEditor");
        if (generator == null)
        {
          ourLogger.Warn("Cannot find UxmlSchemaGenerator type");
          return false;
        }
      }

      var updateSchemaFiles = generator.GetMethod("UpdateSchemaFiles", BindingFlags.Public | BindingFlags.Static);
      if (updateSchemaFiles == null)
      {
        ourLogger.Warn("Cannot find method: UxmlSchemaGenerator.UpdateSchemaFiles");
        return false;
      }

      try
      {
        updateSchemaFiles.Invoke(null, null);
        return true;
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Error trying to generate UIElementsSchema");
        return false;
      }
    }
  }
}