using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Mono.Debugger.Soft;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public class GameObjectComponentsSource : SyntheticGroupObjectValueSourceBase
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<GameObjectComponentsSource>();

        private readonly Value myGameObject;

        public GameObjectComponentsSource(SoftEvaluationContext context, IDebuggerHierarchicalObject parentSource,
                                          Value gameObject)
            : base(context, parentSource, "Components", ourLogger)
        {
            myGameObject = gameObject;
        }

        protected override ObjectValue[] GetChildrenSafe(ObjectPath path, int index, int count,
                                                         IEvaluationOptions options)
        {
            // Call gameObject.GetComponents(typeof(Component))
            var componentType = GetType("UnityEngine.Component");
            var typeofComponent = Adaptor.CreateTypeObject(Context, componentType);

            var componentsArray =
                InvokeInstanceMethod(myGameObject, "GetComponents", typeofComponent) as ArrayMirror;
            if (componentsArray == null)
            {
                ourLogger.Warn("UnityEngine.Component.GetComponents did not return an instance of ArrayMirror");
                return EmptyArray<ObjectValue>.Instance;
            }

            // Component name comes from ObjectNames.GetInspectorTitle(component)
            var objectNamesType = GetType("UnityEditor.ObjectNames");

            var objectValues = new List<ObjectValue>(componentsArray.Length);
            foreach (var componentValue in componentsArray.GetValues(0, componentsArray.Length))
            {
                try
                {
                    var name = GetComponentName(objectNamesType, componentValue);
                    objectValues.Add(CreateObjectValue(name, componentValue, options));
                }
                catch (Exception e)
                {
                    ourLogger.Error(e, "Failed to fetch component {0} of GameObject {1}", componentValue, myGameObject);
                }
            }

            return objectValues.ToArray();
        }

        private string GetComponentName([CanBeNull] TypeMirror objectNamesType, Value componentValue)
        {
            if (objectNamesType != null)
            {
                try
                {
                    var result = InvokeStaticMethod(objectNamesType, "GetInspectorTitle", componentValue);
                    if (result is StringMirror stringMirror)
                        return stringMirror.Value;
                }
                catch (Exception e)
                {
                    ourLogger.Error(e, "Unable to fetch object names for {0}", componentValue);
                }
            }

            return componentValue.Type.Name;
        }
    }
}