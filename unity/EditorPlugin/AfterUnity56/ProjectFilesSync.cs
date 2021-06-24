using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Base;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting;

namespace JetBrains.Rider.Unity.Editor
{
    /// <summary>
    /// If: EditorPlugin is not present, RiderPackage would save to disk (old behaviour)
    /// Else: RiderPackage writes them to a ScriptableObject and fires Changed event.
    /// On every EditorPlugin init we try to process Queue and subscribe to Changed.
    /// </summary>
    // DO NOT Rename or move this type, its presence is checked from RiderPackage with reflection 
    [UsedImplicitly]
    public static class ProjectFilesSync
    {
        private static readonly ILog ourLogger = Log.GetLog("ProjectFilesSync");

        public static void Sync(BackendUnityModel model, Lifetime lifetime)
        {
            var assembly = RiderPackageInterop.GetAssembly();
            if (assembly == null)
            {
                ourLogger.Error("EditorPlugin assembly is null.");
                return;
            }

            var data = assembly.GetType("Packages.Rider.Editor.ProjectFiles.ProjectFilesSyncData");
            if (data == null) return;

            ProcessQueue(data, model);

            SubscribeToChanged(data, model, lifetime);
        }


        private static void SubscribeToChanged(Type data, BackendUnityModel model, Lifetime lifetime)
        {
            var eventInfo = data.GetEvent("Changed");

            if (eventInfo != null)
            {
                var handler = new EventHandler((sender, e) => { ProcessQueue(data, model); });
                eventInfo.AddEventHandler(handler.Target, handler);
                AppDomain.CurrentDomain.DomainUnload += (_, __) =>
                {
                    eventInfo.RemoveEventHandler(handler.Target, handler);
                };
                lifetime.OnTermination(() => { eventInfo.RemoveEventHandler(handler.Target, handler); });
            }
            else
            {
                ourLogger.Error("Changed event subscription failed.");
            }
        }

        private static void ProcessQueue(Type data, BackendUnityModel model)
        {
            if (!model.IsBound)
                return;

            var baseType = data.BaseType;
            if (baseType == null) return;
            var instance = baseType.GetProperty("instance");
            if (instance == null) return;
            var instanceVal = instance.GetValue(null, new object[] { });

            var listField = data.GetField("events");
            if (listField == null) return;
            var list = listField.GetValue(instanceVal);

            var events = (IEnumerable) list;

            var dict = new Dictionary<string, string>();
            foreach (var ev in events)
            {
                var path = (string) ev.GetType().GetField("path").GetValue(ev);
                var content = (string) ev.GetType().GetField("content").GetValue(ev);
                if (dict.ContainsKey(path))
                {
                    ourLogger.Verbose($"Drop change of {path} in favour of newer one");
                    dict[path] = content;
                }
                else
                    dict.Add(path, content);
            }

            model.FileChanges.SetValue(dict.Select(a => new FileChangeArgs(a.Key, a.Value)).ToList());

            var clearMethod = data.GetMethod("Clear");
            clearMethod?.Invoke(instanceVal, new object[] { });
        }
    }
}