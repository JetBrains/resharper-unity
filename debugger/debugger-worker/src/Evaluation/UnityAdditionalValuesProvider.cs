using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityAdditionalValuesProvider : UnityAdditionalValuesProvider<Value>
    {
        public UnityAdditionalValuesProvider(IDebuggerSession session, IValueServicesFacade<Value> valueServices,
                                             IUnityOptions unityOptions, ILogger logger)
            : base(session, valueServices, unityOptions, logger)
        {
        }
    }

    public class UnityAdditionalValuesProvider<TValue> : IAdditionalValuesProvider
        where TValue : class
    {
        private readonly IDebuggerSession mySession;
        private readonly IValueServicesFacade<TValue> myValueServices;
        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        protected UnityAdditionalValuesProvider(IDebuggerSession session, IValueServicesFacade<TValue> valueServices,
                                                IUnityOptions unityOptions, ILogger logger)
        {
            mySession = session;
            myValueServices = valueServices;
            myUnityOptions = unityOptions;
            myLogger = logger;
        }

        public IEnumerable<IValueEntity> GetAdditionalLocals(IStackFrame frame)
        {
            if (!myUnityOptions.ExtensionsEnabled)
                yield break;

            // Add "Active Scene" as a top level item to mimic the Hierarchy window in Unity
            var activeScene = GetActiveScene(frame);
            if (activeScene != null)
                yield return activeScene.ToValue(myValueServices);

            // If `this` is a MonoBehaviour, promote `this.gameObject` to top level to make it easier to find,
            // especially if inherited properties are hidden
            var thisGameObject = GetThisGameObjectForMonoBehaviour(frame);
            if (thisGameObject != null)
                yield return thisGameObject.ToValue(myValueServices);
        }

        [CanBeNull]
        private IValueReference<TValue> GetActiveScene(IStackFrame frame)
        {
            return myLogger.CatchEvaluatorException<TValue, IValueReference<TValue>>(() =>
                {
                    // Make sure we can evaluate. This is set automatically for GetChildren. We'll allow it here, too.
                    // If the user wishes to stop this, they can disable specific settings in the Unity settings page
                    var newOptions = mySession.EvaluationOptions.WithOverridden(o => o.AllowTargetInvoke = true);

                    var sceneManagerType = myValueServices.GetReifiedType(frame,
                                               "UnityEngine.SceneManagement.SceneManager, UnityEngine.CoreModule")
                                           ?? myValueServices.GetReifiedType(frame,
                                               "UnityEngine.SceneManagement.SceneManager, UnityEngine");
                    if (sceneManagerType == null)
                    {
                        myLogger.Warn("Unable to get typeof(SceneManager). Not a Unity project?");
                        return null;
                    }

                    var getActiveSceneMethod = sceneManagerType.MetadataType.GetMethods()
                        .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0 && m.Name == "GetActiveScene");
                    if (getActiveSceneMethod == null)
                    {
                        myLogger.Warn("Unable to find SceneManager.GetActiveScene method");
                        return null;
                    }

                    // GetActiveScene can throw a UnityException if we call it from the wrong location, such as the
                    // constructor of a MonoBehaviour
                    var activeScene =
                        sceneManagerType.CallStaticMethod(frame, newOptions, getActiveSceneMethod);
                    if (activeScene == null)
                    {
                        myLogger.Warn("Unexpected response: SceneManager.GetActiveScene() == null");
                        return null;
                    }

                    // Don't show type presentation. We know it's a scene, the clue's in the name
                    return new SimpleValueReference<TValue>(activeScene, sceneManagerType.MetadataType,
                        "Active scene", ValueOriginKind.Property,
                        ValueFlags.None | ValueFlags.IsReadOnly | ValueFlags.IsDefaultTypePresentation, frame,
                        myValueServices.RoleFactory);
                },
                exception => myLogger.LogThrownUnityException(exception, frame, myValueServices, mySession.EvaluationOptions));
        }

        [CanBeNull]
        private IValueReference<TValue> GetThisGameObjectForMonoBehaviour(IStackFrame frame)
        {
            return myLogger.CatchEvaluatorException<TValue, IValueReference<TValue>>(() =>
                {
                    // Make sure we can evaluate. The debugger overrides this for GetChildren, we'll allow it here, too.
                    // If the user wishes to stop this, they can disable specific settings in the Unity settings page
                    var newOptions = mySession.EvaluationOptions.WithOverridden(o => o.AllowTargetInvoke = true);

                    var thisObj = frame.GetThis(newOptions);
                    if (thisObj?.DeclaredType?.FindTypeThroughHierarchy("UnityEngine.MonoBehaviour") == null)
                        return null;

                    if (!(thisObj.GetPrimaryRole(newOptions) is IObjectValueRole<TValue> role))
                    {
                        myLogger.Warn("Unable to get 'this' as object value");
                        return null;
                    }

                    var gameObjectReference = role.GetInstancePropertyReference("gameObject", true);
                    if (gameObjectReference == null)
                    {
                        myLogger.Warn("Unable to find 'this.gameObject' as a property reference");
                        return null;
                    }

                    // There's a chance that `gameObject` will throw a UnityException because we're not allowed to call
                    // it here (e.g. MonoBehaviour ctor), so invoke the method now rather than returning a decorated
                    // version of the property value reference. We'll catch the exception and react gracefully. Note
                    // that if the gameObject property returned null (it won't), we'd still get a valid value here.
                    var gameObject = gameObjectReference.GetValue(newOptions);
                    var gameObjectType = gameObjectReference.GetValueType(newOptions,
                        myValueServices.ValueMetadataProvider);

                    // Don't show type for each child game object. It's always "GameObject", and we know they're game
                    // objects from the synthetic group.
                    return new SimpleValueReference<TValue>(gameObject, gameObjectType, "this.gameObject",
                        ValueOriginKind.Property,
                        ValueFlags.None | ValueFlags.IsDefaultTypePresentation | ValueFlags.IsReadOnly, frame,
                        myValueServices.RoleFactory);
                },
                exception => myLogger.LogThrownUnityException(exception, frame, myValueServices, mySession.EvaluationOptions));
        }
    }
}