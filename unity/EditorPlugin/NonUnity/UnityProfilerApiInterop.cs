using System;
using System.IO;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    public static class UnityProfilerApiInterop
    {
        public static void StartProfiling(string dllFile, bool needReloadScripts)
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
            
            if (needReloadScripts)
                ReloadScripts();
        }

        private static void ReloadScripts()
        {
#if UNITY_2019_3_OR_NEWER
            EditorUtility.RequestScriptReload(); // EditorPlugin would get loaded
#else 
            UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
#endif
        }
    }
}
