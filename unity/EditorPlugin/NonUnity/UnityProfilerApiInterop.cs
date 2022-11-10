using System;
using System.IO;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    public static class UnityProfilerApiInterop
    {
        public static void StartProfiling(string dllFile, bool needReloadScripts = false)
        {
            const string method = "StartProfiling";
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
                Debug.Log($"{method}: {dllFile}");
            
            InvokeApi(dllFile, method);
            
            if (needReloadScripts)
                ReloadScripts();
        }

        public static void StopProfiling(string dllFile)
        {
            const string method = "StopProfiling";
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
                Debug.Log($"{method}: {dllFile}");

            InvokeApi(dllFile, method);
        }

        private static void ReloadScripts()
        {
#if UNITY_2019_3_OR_NEWER
            EditorUtility.RequestScriptReload(); // EditorPlugin would get loaded
#else 
            UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
#endif
        }
        
        private static void InvokeApi(string apiPath, string methodName)
        {
            // C:\Work\dotnet-products\Bin.RiderBackend\JetBrains.Etw.UnityProfilerApi.dll
            var assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(apiPath));

            var profilerApiType = assembly.GetType("JetBrains.Etw.Api.UnityProfilerApi");
            if (profilerApiType == null)
                throw new ApplicationException("Unable to get the type");

            var folder = new FileInfo(apiPath).Directory;
            if (folder == null)
                throw new ApplicationException($"Folder of {apiPath} is null");
            
            var instance = Activator.CreateInstance(profilerApiType, folder.FullName);
            
            var method = profilerApiType.GetMethod(methodName);
            if (method == null)
                throw new ApplicationException("Unable to get the method");
            
            method.Invoke(instance, null);
        }
    }
}
