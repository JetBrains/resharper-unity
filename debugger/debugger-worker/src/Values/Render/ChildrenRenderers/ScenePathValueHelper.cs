using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences;
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
                // TODO: Hand roll this for players. Simply follow transform.parent
                // However, this will obviously be more expensive to calculate
                var frame = gameObjectRole.ValueReference.OriginatingFrame;
                var animationUtilityType =
                    valueServices.GetReifiedType(frame, "UnityEditor.AnimationUtility, UnityEditor")
                    ?? valueServices.GetReifiedType(frame, "UnityEditor.AnimationUtility, UnityEditor.CoreModule");
                var method = animationUtilityType?.MetadataType.GetMethods()
                    .FirstOrDefault(ourCalculateTransformPathSelector);
                if (method == null)
                {
                    logger.Trace("Unable to get metadata for AnimationUtility.CalculateTransformPath method. Is this a player?");
                    return null;
                }

                var targetTransformReference = gameObjectRole.GetInstancePropertyReference("transform");
                var targetTransformRole = targetTransformReference?.AsObjectSafe(options);
                // Search in bases - transform might be a RectTransform or a Transform, and root is defined on Transform
                var rootTransformReference = targetTransformRole?.GetInstancePropertyReference("root", true);

                if (targetTransformReference == null || rootTransformReference == null)
                {
                    logger.Warn("Unable to evaluate gameObject.transform and/or gameObject.root. Unexpected.");
                    return null;
                }

                var rootTransformName = rootTransformReference.AsObjectSafe(options)
                    ?.GetInstancePropertyReference("name", true)
                    ?.AsStringSafe(options)?.GetString() ?? "";

                var pathValue = animationUtilityType.CallStaticMethod(frame, options, method,
                    targetTransformReference.GetValue(options), rootTransformReference.GetValue(options));
                var path = new SimpleValueReference<TValue>(pathValue, frame, valueServices.RoleFactory)
                    .AsStringSafe(options)?.GetString();
                if (path == null)
                {
                    // We expect empty string at least
                    logger.Warn("Unexpected null returned from AnimationUtility.CalculateTransformPath");
                    return null;
                }

                var fullPath = path.IsNullOrEmpty() ? rootTransformName : rootTransformName + "/" + path;
                var fullPathValue = valueServices.ValueFactory.CreateString(frame, options, fullPath);

                // Don't show type presentation. This is informational, rather than an actual property
                var simpleReference = new SimpleValueReference<TValue>(fullPathValue, null, "Scene path",
                    ValueOriginKind.Property,
                    ValueFlags.None | ValueFlags.IsReadOnly | ValueFlags.IsDefaultTypePresentation, frame,
                    valueServices.RoleFactory);

                // Wrap the simple reference - the default StringValuePresenter will display the simple reference as a
                // string property, with syntax colouring, quotes and type name. Our TextValuePresenter will handle the
                // TextValueReference and use the flags we've set
                return new TextValueReference<TValue>(simpleReference, valueServices.RoleFactory).ToValue(valueServices);
            }
            catch (Exception e)
            {
                logger.Error(e);
                return null;
            }
        }
    }
}