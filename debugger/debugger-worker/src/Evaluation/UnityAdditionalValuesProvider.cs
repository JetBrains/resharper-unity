using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityAdditionalValuesProvider<TValue> : IAdditionalValuesProvider<TValue>
        where TValue : class
    {
        private readonly IDebuggerSession mySession;
        private readonly IValueServicesFacade<TValue> myValueServices;
        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        public UnityAdditionalValuesProvider(IDebuggerSession session, IValueServicesFacade<TValue> valueServices,
                                             IUnityOptions unityOptions, ILogger logger)
        {
            // We can't use EvaluationOptions here, it hasn't been set yet
            mySession = session;
            myValueServices = valueServices;
            myUnityOptions = unityOptions;
            myLogger = logger;
        }

        public IEnumerable<IValueReference<TValue>> GetAdditionalLocals(IStackFrame frame)
        {
            if (!myUnityOptions.ExtensionsEnabled)
                yield break;

            // Add "Active Scene" as a top level item to mimic the Hierarchy window in Unity
            var activeScene = GetActiveScene(frame);
            if (activeScene != null)
                yield return activeScene;

            // If `this` is a MonoBehaviour, promote `this.gameObject` to top level to make it easier to find,
            // especially if inherited properties are hidden
            var thisGameObject = GetThisGameObjectForMonoBehaviour(frame);
            if (thisGameObject != null)
                yield return thisGameObject;
        }

        [CanBeNull]
        private IValueReference<TValue> GetActiveScene(IStackFrame frame)
        {
            try
            {
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
                    myLogger.Warn("Unable to find SceneManager.GetActiveScene");
                    return null;
                }

                var activeScene = sceneManagerType.CallStaticMethod(frame, mySession.EvaluationOptions, getActiveSceneMethod);
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
            }
            catch (Exception e)
            {
                myLogger.LogException(e);
                return null;
            }
        }

        [CanBeNull]
        private IValueReference<TValue> GetThisGameObjectForMonoBehaviour(IStackFrame frame)
        {
            try
            {
                var thisObj = frame.GetThis(mySession.EvaluationOptions);
                if (thisObj?.DeclaredType?.FindTypeThroughHierarchy("UnityEngine.MonoBehaviour") != null)
                {
                    if (!(thisObj.GetPrimaryRole(mySession.EvaluationOptions) is IObjectValueRole<TValue> role))
                    {
                        myLogger.Warn("Unable to get 'this' as object value");
                        return null;
                    }

                    var gameObjectReference = role.GetInstancePropertyReference("gameObject", true);
                    if (gameObjectReference == null)
                    {
                        myLogger.Warn("Unable to get 'this.gameObject' as a property reference");
                        return null;
                    }

                    var gameObject = gameObjectReference.GetValue(mySession.EvaluationOptions);
                    var gameObjectType = gameObjectReference.GetValueType(mySession.EvaluationOptions,
                        myValueServices.ValueMetadataProvider);

                    // Don't show type for each child game object. It's always "GameObject", and we know they're game
                    // objects from the synthetic group.
                    return new SimpleValueReference<TValue>(gameObject, gameObjectType, "this.gameObject",
                        ValueOriginKind.Property,
                        ValueFlags.None | ValueFlags.IsDefaultTypePresentation | ValueFlags.IsReadOnly, frame,
                        myValueServices.RoleFactory);
                }
            }
            catch (Exception ex)
            {
                myLogger.LogException(ex);
            }

            return null;
        }
    }
}