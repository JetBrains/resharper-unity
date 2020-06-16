using System.Collections.Generic;
using System.Linq;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class ActiveSceneAdditionalValuesProvider<TValue> : IAdditionalValuesProvider<TValue>
        where TValue : class
    {
        private readonly IValueServicesFacade<TValue> myValueServices;
        private readonly ILogger myLogger;

        public ActiveSceneAdditionalValuesProvider(IValueServicesFacade<TValue> valueServices, ILogger logger)
        {
            myValueServices = valueServices;
            myLogger = logger;
        }

        public IEnumerable<IValueReference<TValue>> GetAdditionalLocals(IStackFrame frame)
        {
            var sceneManagerType = myValueServices.GetReifiedType(frame,
                                       "UnityEngine.SceneManagement.SceneManager, UnityEngine.CoreModule")
                                   ?? myValueServices.GetReifiedType(frame,
                                       "UnityEngine.SceneManagement.SceneManager, UnityEngine");
            if (sceneManagerType == null)
            {
                myLogger.Warn("Unable to get typeof(SceneManager). Not a Unity project?");
                yield break;
            }

            var getActiveSceneMethod = sceneManagerType.MetadataType.GetMethods()
                .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0 && m.Name == "GetActiveScene");
            if (getActiveSceneMethod == null)
            {
                myLogger.Warn("Unable to find SceneManager.GetActiveScene");
                yield break;
            }

            // TODO: Where to get a proper version of value fetch options?
            var options = PresentationOptions.Default;
            var activeScene = sceneManagerType.CallStaticMethod(frame, options, getActiveSceneMethod);
            if (activeScene == null)
            {
                myLogger.Warn("Unexpected response: SceneManager.GetActiveScene() == null");
                yield break;
            }

            yield return new SimpleValueReference<TValue>(activeScene, sceneManagerType.MetadataType,
                "Active Scene", ValueOriginKind.Property, ValueFlags.None, frame, myValueServices.RoleFactory);
        }
    }
}