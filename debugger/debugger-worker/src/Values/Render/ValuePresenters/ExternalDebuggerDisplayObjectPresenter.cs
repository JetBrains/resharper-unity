using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences;
using JetBrains.Util;
using MetadataLite.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.Render.ValuePresenters;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;
using Mono.Debugging.Utils;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ValuePresenters
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class ExternalDebuggerDisplayObjectPresenter<TValue> : ValuePresenterBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        private readonly IDictionary<string, string> myDebuggerDisplayValues = new Dictionary<string, string>
        {
            // Various types have a default ToString implementation that use F1 or F2 to format the float components.
            // This is a huge loss of precision while debugging.
            // Note that formatting is based on the existing ToString implementations, even if it means inconsistency.
            // Where possible, the underlying field is used, so we can still show something if property evaluation is
            // unavailable.
            // Color uses F3. Matrix4x4 uses F5. Arguably, this is enough
            {"UnityEngine.Bounds", "Center: {m_Center}, Extents: {m_Extents}"},
            {"UnityEngine.Plane", "(normal:{m_Normal}, distance:{m_Distance})"},
            {"UnityEngine.Ray", "Origin: {m_Origin}, Dir: {m_Direction}"},
            {"UnityEngine.Ray2D", "Origin: {m_Origin}, Dir: {m_Direction}"},
            {"UnityEngine.Rect", "(x:{m_XMin}, y:{m_YMin}, width:{m_Width}, height:{m_Height})"},
            {"UnityEngine.RectOffset", "RectOffset (l:{left} r:{right} t:{top} b:{bottom})"},
            {"UnityEngine.Vector2", "({x}, {y})"},
            {"UnityEngine.Vector3", "({x}, {y}, {z})"},
            {"UnityEngine.Vector4", "({x}, {y}, {z}, {w})"},

            // Default is ({x}, {y}, {z}, {w}) to F1 precision. Euler angles is more useful
            {"UnityEngine.Quaternion", "eulerAngles: {eulerAngles}"},
            {"UnityEngine.MeshFilter", "vertex count: {sharedMesh.vertexCount}"},
            {"UnityEngine.SceneManagement.Scene", "{name} ({path})"},

            // Local values, as shown in the Inspector
            // We don't show name, as the component name is the same as GameObject name, and isn't as useful in a
            // debugger context
            {"UnityEngine.Transform", "pos: {localPosition} rot: {localRotation} scale: {localScale}"},

            // Default implementation is implemented in native code, so not 100% sure what it does, but it seems to only
            // show "Name (UnityEngine.GameObject)". Note that we override this setting in the synthetic list of game
            // objects. See GameObjectChildrenRenderer
            {"UnityEngine.GameObject", "{name} (active: {activeInHierarchy}, layer: {layer})"}
        };

        // Alternative debugger display strings for when the key is the same as the value's name, in which case, we
        // don't want to include the name in the value as well ("My awesome thing = {GameObject} My awesome thing (...)")
        // This is only used in the synthetic list of child game objects. It's not worth allocating a whole new
        // dictionary for this. Special case it until we have more instances.
        private const string GameObjectDebuggerDisplayStringWithoutName = "active: {activeInHierarchy}, layer: {layer}";

        public ExternalDebuggerDisplayObjectPresenter(IUnityOptions unityOptions, ILogger logger)
        {
            myUnityOptions = unityOptions;
            myLogger = logger;
        }

        [Injected]
        public IExpressionEvaluators<TValue> ExpressionEvaluators { get; protected internal set; }

        public override int Priority => UnityRendererUtil.ValuePresenterPriority;

        public override bool IsApplicable(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType,
                                          IPresentationOptions options,
                                          IUserDataHolder dataHolder)
        {
            // Note that DebuggerDisplayObjectPresenter checks options.AllowTargetInvoke here
            return myUnityOptions.ExtensionsEnabled && options.AllowDebuggerDisplayEvaluation &&
                   myDebuggerDisplayValues.ContainsKey(instanceType.GetGenericTypeDefinition().FullName);
        }

        public override IValuePresentation PresentValue(IObjectValueRole<TValue> valueRole,
                                                        IMetadataTypeLite instanceType,
                                                        IPresentationOptions options, IUserDataHolder dataHolder,
                                                        CancellationToken token)
        {
            var type = instanceType.GetGenericTypeDefinition();
            var debuggerDisplayString = GetDebuggerDisplayString(valueRole, type);
            {
                try
                {
                    var valueReference = valueRole.ValueReference;
                    var thisObj = valueReference.GetValue(options);
                    var evaluationOptions =
                        valueReference.OriginatingFrame.DebuggerSession.Options.EvaluationOptions.Apply(options);
                    var displayString =
                        ExpressionEvaluators.EvaluateDisplayString(valueReference.OriginatingFrame, thisObj,
                            debuggerDisplayString, evaluationOptions, token);
                    return SimplePresentation.CreateSuccess(
                        ValuePresentationPart.Default(DisplayStringUtil.EscapeString(displayString)),
                        valueReference.DefaultFlags, instanceType, displayString);
                }
                catch (Exception ex)
                {
                    myLogger.Error(ex,
                        $"Unable to evaluate debugger display string for type ${type.FullName}: ${debuggerDisplayString}");
                }
            }

            return SimplePresentation.EmptyPresentation;
        }

        [CanBeNull]
        private string GetDebuggerDisplayString(IObjectValueRole<TValue> valueRole, IMetadataTypeLite type)
        {
            // Special case. Replace with a second dictionary or whatever if we need to handle more types
            if (valueRole.ValueReference is NamedReferenceDecorator<TValue> reference && reference.IsNameFromValue
                && type.FullName == "UnityEngine.GameObject")
            {
                return GameObjectDebuggerDisplayStringWithoutName;
            }

            if (myDebuggerDisplayValues.TryGetValue(type.FullName, out var debuggerDisplayString))
                return debuggerDisplayString;

            return null;
        }
    }
}
