using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Diagnostics;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    public static class Kernel32Dll
    {
        public static void StartProfiling(string dllFile)
        {
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
                Debug.Log($"StartProfiling: {dllFile}");

            // C:\Work\dotnet-products\Bin.RiderBackend\JetBrains.Etw.UnityProfilerApi.dll
            var assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(dllFile));

            var type = assembly.GetType("JetBrains.Etw.Api.UnityProfilerApi");
            if (type == null)
                throw new ApplicationException("Unable to get the type");

            var folder = new FileInfo(dllFile).Directory;
            if (folder == null)
                throw new ApplicationException($"Folder of {dllFile} is null");
            var instance = Activator.CreateInstance(type, folder.FullName);

            var method = type.GetMethod("StartProfiling");
            if (method == null)
                throw new ApplicationException("Unable to get the method");

            method.Invoke(instance, null); // call StartProfiling
        }
    }
}