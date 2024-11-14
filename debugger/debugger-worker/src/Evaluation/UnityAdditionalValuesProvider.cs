using System.Collections.Generic;
using System.Linq;
using JetBrains.Debugger.Worker.Plugins.Unity.Values;
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
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.MetadataLite.Services;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityAdditionalValuesProvider : UnityAdditionalValuesProvider<Value>
    {
        public UnityAdditionalValuesProvider(IDebuggerSession session, IValueServicesFacade<Value> valueServices,
                                             IUnityOptions unityOptions, ILogger logger, IValueFactory<Value> factory)
            : base(session, valueServices, unityOptions, logger, factory)
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
        private readonly IValueFactory<TValue> myFactory;

        protected UnityAdditionalValuesProvider(IDebuggerSession session, IValueServicesFacade<TValue> valueServices,
                                                IUnityOptions unityOptions, ILogger logger, IValueFactory<TValue> factory)
        {
            mySession = session;
            myValueServices = valueServices;
            myUnityOptions = unityOptions;
            myLogger = logger;
            myFactory = factory;
        }

        public IEnumerable<IValueEntity> GetAdditionalLocals(IStackFrame frame)
        {
            // Do nothing if "Allow property evaluations..." option is disabled.
            // The debugger works in two steps - get value entities/references, and then get value presentation.
            // Evaluation is always allowed in the first step, but depends on user options for the second. This allows
            // evaluation to calculate children, e.g. expanding the Results node of IEnumerable, but presentation might
            // require clicking "refresh". We should be returning un-evaluated value references here.
            // TODO: Make "Active Scene" and "this.gameObject" lazy in 212
            if (!myUnityOptions.ExtensionsEnabled || !mySession.EvaluationOptions.AllowTargetInvoke)
                yield break;

            // Add "Active Scene" as a top level item to mimic the Hierarchy window in Unity
            var activeScene = GetActiveScene(frame);
            if (activeScene != null)
                yield return activeScene;

            var dontDestroyOnLoadScene = GetDontDestroyOnLoadScene(frame);
            if(dontDestroyOnLoadScene != null)
                yield return dontDestroyOnLoadScene;

            // If `this` is a MonoBehaviour, promote `this.gameObject` to top level to make it easier to find,
            // especially if inherited properties are hidden
            var thisGameObject = GetThisGameObjectForMonoBehaviour(frame);
            if (thisGameObject != null)
                yield return thisGameObject.ToValue(myValueServices);
        }

        private IValueEntity? GetDontDestroyOnLoadScene(IStackFrame frame)
        {
            return myLogger.CatchEvaluatorException<TValue, IValueEntity?>(() =>
                {
                    var editorSceneManagerType = GetEditorSceneManagerType(frame);
                    if (editorSceneManagerType == null)
                        return null;
                    var getDontDestroyOnLoadMethod = GetDontDestroyOnLoadSceneMethod(editorSceneManagerType);
                    if (getDontDestroyOnLoadMethod == null)
                        return null;


                    var dontDestroyOnLoadScene =
                        editorSceneManagerType.CallStaticMethod(frame, mySession.EvaluationOptions,
                            getDontDestroyOnLoadMethod);
                    if (dontDestroyOnLoadScene == null)
                    {
                        myLogger.Warn("Unexpected response: EditorSceneManager.GetDontDestroyOnLoadScene() == null");
                        return null;
                    }

                    return CreateValueEntity(dontDestroyOnLoadScene, "DontDestroyOnLoad Scene", editorSceneManagerType.MetadataType, frame);
                },
                exception =>
                    myLogger.LogThrownUnityException(exception, frame, myValueServices, mySession.EvaluationOptions));
        }
        
        private IValueEntity? GetActiveScene(IStackFrame frame)
        {
            return myLogger.CatchEvaluatorException<TValue, IValueEntity?>(() =>
                {
                    var sceneManagerType = GetSceneManagerType(frame);
                    if (sceneManagerType == null)
                        return null;

                    var getActiveSceneMethod = GetActiveSceneMethod(sceneManagerType);
                    if (getActiveSceneMethod == null)
                        return null;

                    var getSceneCountProperty = GetSceneCountProperty(frame, sceneManagerType);

                    var activeScene = GetActiveSceneValue(frame, sceneManagerType, getActiveSceneMethod);
                    
                    if (getSceneCountProperty == null)
                        return CreateValueEntity(activeScene, "Active scene", sceneManagerType.MetadataType, frame);

                    var scenesCount = getSceneCountProperty.AsPrimitive(mySession.EvaluationOptions).GetPrimitiveSafe<int>();

                    var getSceneAtMethod = GetSceneAtMethod(sceneManagerType);

                    if(getSceneAtMethod == null || scenesCount == null || scenesCount == 1)
                        return CreateValueEntity(activeScene, "Active scene", sceneManagerType.MetadataType, frame);

                    var simpleValueReferences = GetLoadedScenes(frame, sceneManagerType, getSceneAtMethod, scenesCount.Value, activeScene);
                    return new SimpleEntityGroup("Loaded Scenes", simpleValueReferences, true);

                },
                exception => myLogger.LogThrownUnityException(exception, frame, myValueServices, mySession.EvaluationOptions));
        }

        private IValue<TValue> CreateValueEntity(TValue activeScene, string defaultName, IMetadataTypeLite metadataTypeLite, IStackFrame frame)
        {
            return new SimpleValueReference<TValue>(activeScene, metadataTypeLite,
                defaultName, ValueOriginKind.Property,
                ValueFlags.None | ValueFlags.IsReadOnly | ValueFlags.IsDefaultTypePresentation, frame,
                myValueServices.RoleFactory).ToValue(myValueServices);
        }

        private List<IValueEntity> GetLoadedScenes(IStackFrame frame, IReifiedType<TValue> sceneManagerType,
            IMetadataMethodLite getSceneAtMethod, int scenesCount, TValue activeScene)
        {
            var activeSceneHandle = TryGetActiveSceneHandle(activeScene);

            var loadedScenes = new List<IValueEntity>();
            for (int i = 0; i < scenesCount; i++)
            {
                var indexValue = myFactory.CreatePrimitive(frame, mySession.EvaluationOptions, i);
                var sceneAtIndex =
                    sceneManagerType.CallStaticMethod(frame,
                        mySession.EvaluationOptions,
                        getSceneAtMethod, indexValue);

                if (sceneAtIndex == null)
                {
                    myLogger.Warn($"Can't get active scene by index: {i}");
                    continue;
                }

                var defaultName = i.ToString();

                var sceneAtIndexHandle = TryGetActiveSceneHandle(sceneAtIndex);

                if (sceneAtIndexHandle != 0 && sceneAtIndexHandle == activeSceneHandle)
                    defaultName += " (Active)";

                loadedScenes.Add(CreateValueEntity(sceneAtIndex, defaultName, sceneManagerType.MetadataType, frame));
            }

            return loadedScenes;

            int TryGetActiveSceneHandle(TValue sceneValue)
            {
                // Unity Scene struct has a handle field, which is a primitive int.
                // We can use it to check if the scene is active.
                var sceneHandle = ((sceneValue as StructMirror)?.Fields[0] as PrimitiveValue)?.Value;

                if (sceneHandle is int intHandle)
                    return intHandle;
                
                //handle comes from native code, 0 - means null
                return 0;
            }
        }

        private IMetadataMethodLite? GetDontDestroyOnLoadSceneMethod(IReifiedType<TValue>? editorSceneManagerType)
        {
            var getDontDestroyOnLoadSceneMethod = editorSceneManagerType?.MetadataType.GetMethods()
                .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0 && m.Name == "GetDontDestroyOnLoadScene");
            
            if(getDontDestroyOnLoadSceneMethod == null)
                myLogger.Warn("Unable to find EditorSceneManager.GetDontDestroyOnLoadScene method");
            
            return getDontDestroyOnLoadSceneMethod;
        }

        private TValue GetActiveSceneValue(IStackFrame frame, IReifiedType<TValue> sceneManagerType,
            IMetadataMethodLite getActiveSceneMethod)
        {
            // GetActiveScene can throw a UnityException if we call it from the wrong location, such as the
            // constructor of a MonoBehaviour
            var activeScene =
                sceneManagerType.CallStaticMethod(frame, mySession.EvaluationOptions, getActiveSceneMethod);
            if (activeScene == null)
            {
                myLogger.Warn("Unexpected response: SceneManager.GetActiveScene() == null");
                return null;
            }


            return activeScene;
        }

        private IPropertyValueReference<TValue>? GetSceneCountProperty(IStackFrame frame,
            IReifiedType<TValue> sceneManagerType)
        {
            var sceneCountProperty = sceneManagerType.GetStaticProperties(frame, p => p.Name == "sceneCount")
                .FirstOrDefault();
            if(sceneCountProperty is IPropertyValueReference<TValue> sceneCountPropertyReference)
                return sceneCountPropertyReference;
            
            myLogger.Warn("Unablde to find SceneManager.sceneCount property");
            return null;
        }


        private IMetadataMethodLite? GetSceneAtMethod(IReifiedType<TValue> sceneManagerType)
        {
            var getSceneAtMethod = sceneManagerType.MetadataType.GetMethods()
                .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 1 && m.Name == "GetSceneAt");
            if (getSceneAtMethod == null)
            {
                myLogger.Warn("Unable to find SceneManager.GetSceneAt method");
            }

            return getSceneAtMethod;
        }

        private IMetadataMethodLite? GetActiveSceneMethod(IReifiedType<TValue> sceneManagerType)
        {
            var getActiveSceneMethod = sceneManagerType.MetadataType.GetMethods()
                .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0 && m.Name == "GetActiveScene");
            if (getActiveSceneMethod == null)
            {
                myLogger.Warn("Unable to find SceneManager.GetActiveScene method");
            }

            return getActiveSceneMethod;
        }

        private IReifiedType<TValue>? GetSceneManagerType(IStackFrame frame)
        {
            var sceneManagerType = myValueServices.GetReifiedType(frame,
                                       "UnityEngine.SceneManagement.SceneManager, UnityEngine.CoreModule")
                                   ?? myValueServices.GetReifiedType(frame,
                                       "UnityEngine.SceneManagement.SceneManager, UnityEngine");
            if (sceneManagerType == null)
            {
                myLogger.Warn("Unable to get typeof(SceneManager). Not a Unity project?");
            }

            return sceneManagerType;
        }
        private IReifiedType<TValue>? GetEditorSceneManagerType(IStackFrame frame)
        {
            var sceneManagerType = myValueServices.GetReifiedType(frame,
                                       "UnityEditor.SceneManagement.EditorSceneManager, UnityEditor.CoreModule")
                                   ?? myValueServices.GetReifiedType(frame,
                                       "UnityEditor.SceneManagement.EditorSceneManager, UnityEditor");
            if (sceneManagerType == null)
            {
                myLogger.Warn("Unable to get typeof(EditorSceneManager). Not a Unity project?");
            }

            return sceneManagerType;
        }

        private IValueReference<TValue>? GetThisGameObjectForMonoBehaviour(IStackFrame frame)
        {
            return myLogger.CatchEvaluatorException<TValue, IValueReference<TValue>?>(() =>
                {
                    var thisObj = frame.GetThis(mySession.EvaluationOptions);
                    if (thisObj?.DeclaredType?.FindTypeThroughHierarchy("UnityEngine.MonoBehaviour") == null)
                        return null;

                    if (!(thisObj.GetPrimaryRole(mySession.EvaluationOptions) is IObjectValueRole<TValue> role))
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
                    var gameObject = gameObjectReference.GetValue(mySession.EvaluationOptions);
                    var gameObjectType = gameObjectReference.GetValueType(mySession.EvaluationOptions,
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