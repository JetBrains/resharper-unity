/*
 * MIT License

Copyright (c) 2016-2018 GitHub

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using System.Linq;
using JetBrains.Diagnostics;
using UnityEditorInternal;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace JetBrains.Rider.Unity.Editor.Utils
{
  [AttributeUsage(AttributeTargets.Class)]
  public sealed class LocationAttribute : Attribute
  {
    public enum Location { PreferencesFolder, LibraryFolder }
    public string Filepath { get; }

    public LocationAttribute(string relativePath, Location location)
    {
      if (relativePath[0] == '/')
        relativePath = relativePath.Substring(1);

      if (location == Location.PreferencesFolder)
        Filepath = Path.Combine(InternalEditorUtility.unityPreferencesFolder, relativePath);
      else if (location == Location.LibraryFolder)
        Filepath = Path.Combine(Path.GetFullPath("Library"), relativePath);
    }
  }


  public class ScriptObjectSingleton<T> : ScriptableObject where T : ScriptableObject
  {
    private static readonly ILog ourLogger = Log.GetLog("ScriptObjectSingleton");
    private static T instance;
    public static T Instance
    {
      get
      {
        if (instance == null)
          CreateAndLoad();
        return instance;
      }
    }

    protected ScriptObjectSingleton()
    {
      if (instance != null)
      {
        ourLogger.Error("Singleton already exists!");
      }
      else
      {
        instance = this as T;
      }
    }

    private static void CreateAndLoad()
    {
      string filePath = GetFilePath();
      if (!string.IsNullOrEmpty(filePath))
      {
        InternalEditorUtility.LoadSerializedFileAndForget(filePath);
      }

      if (instance == null)
      {
        var inst = CreateInstance<T>() as ScriptObjectSingleton<T>;
        inst.hideFlags = HideFlags.HideAndDontSave;
        inst.Save(true);
      }

      Debug.Assert(instance != null);
    }

    protected void Save(bool saveAsText)
    {
      if (instance == null)
      {
        ourLogger.Error("Cannot save singleton, no instance!");
        return;
      }

      var locationFilePath = GetFilePath();
      if (locationFilePath != null)
      {
        if (!Directory.Exists(locationFilePath))
          Directory.CreateDirectory(Path.GetDirectoryName(locationFilePath));
        InternalEditorUtility.SaveToSerializedFileAndForget(new[] { instance }, locationFilePath, saveAsText);
      }
    }

    private static string GetFilePath()
    {
      var attr = typeof(T).GetCustomAttributes(true)
        .OfType<LocationAttribute>()
        .FirstOrDefault();
      //ourLogger.Verbose("FilePath {0}", attr?.Filepath);
      return attr?.Filepath;
    }
  }
}