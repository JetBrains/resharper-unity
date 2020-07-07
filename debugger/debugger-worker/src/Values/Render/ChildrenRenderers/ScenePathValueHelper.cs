using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Util;
using MetadataLite.API;
using MetadataLite.API.Selectors;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    public static class ScenePathValueHelper
    {
        private static readonly MethodSelector ourCalculateTransformPathSelector = new MethodSelector(m =>
            m.IsStatic && m.Name == "CalculateTransformPath" && m.Parameters.Length == 2
            && m.Parameters[0].Type.Is("UnityEngine.Transform")
            && m.Parameters[1].Type.Is("UnityEngine.Transform"));

        [CanBeNull]
        public static IValueEntity GetScenePathValue<TValue>(IObjectValueRole<TValue> gameObjectRole,
                                                             IPresentationOptions options,
                                                             IValueServicesFacade<TValue> valueServices,
                                                             ILogger logger)
            where TValue : class
        {
            try
            {
                // Only available in the editor. Not available for players, where we'll display nothing.
                // TODO: Hand roll this. Simply follow transform.parent
                var frame = gameObjectRole.ValueReference.OriginatingFrame;
                var animationUtilityType =
                    valueServices.GetReifiedType(frame, "UnityEditor.AnimationUtility, UnityEditor")
                    ?? valueServices.GetReifiedType(frame, "UnityEditor.AnimationUtility, UnityEditor.CoreModule");
                var method = animationUtilityType?.MetadataType.GetMethods()
                    .FirstOrDefault(ourCalculateTransformPathSelector);
                if (method == null)
                    return null;

                var targetTransformReference = gameObjectRole.GetInstancePropertyReference("transform");
                var targetTransformRole = targetTransformReference?.AsObjectSafe(options);
                var rootTransformReference = targetTransformRole?.GetInstancePropertyReference("root");

                if (targetTransformReference == null || rootTransformReference == null)
                    return null;

                var rootTransformName = rootTransformReference.AsObjectSafe(options)
                    ?.GetInstancePropertyReference("name", true)
                    ?.AsStringSafe(options)?.GetString() ?? "";

                var pathValue = animationUtilityType.CallStaticMethod(frame, options, method,
                    targetTransformReference.GetValue(options), rootTransformReference.GetValue(options));
                var path = new SimpleValueReference<TValue>(pathValue, frame, valueServices.RoleFactory)
                    .AsStringSafe(options)?.GetString();
                if (path == null)
                    return null;

                var fullPath = path.IsNullOrEmpty() ? rootTransformName : rootTransformName + "/" + path;
                var fullPathValue = valueServices.ValueFactory.CreateString(frame, options, fullPath);
                return new SimpleValueReference<TValue>(fullPathValue, null, "Scene Path", ValueOriginKind.Property,
                    ValueFlags.None, frame, valueServices.RoleFactory).ToValue(valueServices);
            }
            catch (Exception e)
            {
                logger.Error(e);
                return null;
            }
        }
    }
}