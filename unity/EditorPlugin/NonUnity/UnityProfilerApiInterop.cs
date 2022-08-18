using System;
using System.IO;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    public class UnityProfilerApiInterop
    {
        private static UnityProfilerApiInterop ourInstance;
        
        public static void StartProfiling(string dllFile, bool needReloadScripts)
        {
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
                Debug.Log($"StartProfiling: {dllFile}");

            if(ourInstance != null)
                StopProfiling();
            
            ourInstance = new UnityProfilerApiInterop(dllFile);
            ourInstance.Start();

            if (needReloadScripts)
                ReloadScripts();
        }

        public static void StopProfiling()
        {
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
                Debug.Log($"StopProfiling");

            if (ourInstance == null)
                return;
            
            ourInstance.Stop();
            ourInstance = null;
        }

        private static void ReloadScripts()
        {
#if UNITY_2019_3_OR_NEWER
            EditorUtility.RequestScriptReload(); // EditorPlugin would get loaded
#else 
            UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
#endif
        }

        
        private readonly object myProfilerInstance;
        private readonly Type myProfilerApiType;

        private UnityProfilerApiInterop(string apiPath)
        {
            // C:\Work\dotnet-products\Bin.RiderBackend\JetBrains.Etw.UnityProfilerApi.dll
            var assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(apiPath));

            myProfilerApiType = assembly.GetType("JetBrains.Etw.Api.UnityProfilerApi");
            if (myProfilerApiType == null)
                throw new ApplicationException("Unable to get the type");

            var folder = new FileInfo(apiPath).Directory;
            if (folder == null)
                throw new ApplicationException($"Folder of {apiPath} is null");
            
            myProfilerInstance = Activator.CreateInstance(myProfilerApiType, folder.FullName);
        }

        public void Start() => Invoke("StartProfiling");

        public void Stop() => Invoke("StopProfiling");
        
        private void Invoke(string methodName)
        {
            var method = myProfilerApiType.GetMethod(methodName);
            if (method == null)
                throw new ApplicationException("Unable to get the method");
            
            method.Invoke(myProfilerInstance, null);
        }
    }
}
