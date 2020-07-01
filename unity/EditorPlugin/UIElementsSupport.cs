using System;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class UIElementsSupport
  {
    private static readonly ILog ourLogger = Log.GetLog("UIElementsSupport");

    public static bool GenerateSchema()
    {
      ourLogger.Verbose("Generating UXML schema");

      // This type was first introduced in 2019.1
      var generator = Type.GetType("UnityEditor.UIElements.UxmlSchemaGenerator,UnityEditor")
                      ?? Type.GetType("UnityEditor.UIElements.UxmlSchemaGenerator,UnityEditor.UIElementsModule");
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
        ourLogger.Verbose("Found reflection types, starting to generate UXML schema");
        updateSchemaFiles.Invoke(null, null);
        ourLogger.Verbose("Successfully generated UXML schema");
        return true;
      }
      catch (Exception e)
      {
        Debug.Log("Error trying to generate UIElementsSchema");
        Debug.LogException(e);
        ourLogger.Error(e, "Error trying to generate UIElementsSchema");
        return false;
      }
    }
  }
}