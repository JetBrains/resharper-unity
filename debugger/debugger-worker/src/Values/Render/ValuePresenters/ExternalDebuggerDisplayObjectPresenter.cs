using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using JetBrains.Debugger.Worker.Plugins.Unity.Values.ValueReferences;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.Render.ValuePresenters;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.Soft;
using StatisticsKind = Mono.Debugging.Client.Values.Render.StatisticsKind;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ValuePresenters
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class ExternalDebuggerDisplayObjectPresenter<TValue> : ValuePresenterBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        // This is fine. We're only ever instantiated with one type of TValue
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Key<string> ourDebuggerDisplayStringKey = new Key<string>("DebuggerDisplayString");

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
            { "UnityEngine.Bounds", "Center: {m_Center}, Extents: {m_Extents}" },
            { "UnityEngine.Plane", "(normal:{m_Normal}, distance:{m_Distance})" },
            { "UnityEngine.Ray", "Origin: {m_Origin}, Dir: {m_Direction}" },
            { "UnityEngine.Ray2D", "Origin: {m_Origin}, Dir: {m_Direction}" },
            { "UnityEngine.Rect", "(x:{m_XMin}, y:{m_YMin}, width:{m_Width}, height:{m_Height})" },
            { "UnityEngine.RectOffset", "RectOffset (l:{left} r:{right} t:{top} b:{bottom})" },
            { "UnityEngine.Vector2", "({x}, {y})" },
            { "UnityEngine.Vector3", "({x}, {y}, {z})" },
            { "UnityEngine.Vector4", "({x}, {y}, {z}, {w})" },

            // Default is ({x}, {y}, {z}, {w}) to F1 precision. Euler angles is more useful
            { "UnityEngine.Quaternion", "eulerAngles: {eulerAngles}" },
            { "UnityEngine.Mesh", "vertex count: {vertexCount}" },
            { "UnityEngine.MeshFilter", "shared mesh: ({sharedMesh})" },
            { "UnityEngine.SceneManagement.Scene", "{name} ({path})" },

            // Local values, as shown in the Inspector
            // We don't show name, as the component name is the same as GameObject name, and isn't as useful in a
            // debugger context
            { "UnityEngine.Transform", "pos: {localPosition} rot: {localRotation} scale: {localScale}" },

            // Default implementation is implemented in native code, so not 100% sure what it does, but it seems to only
            // show "Name (UnityEngine.GameObject)". Note that we override this setting in the synthetic list of game
            // objects. See GameObjectChildrenRenderer
            { "UnityEngine.GameObject", "{name} (active: {activeInHierarchy}, layer: {layer})" },

            // Used by Behaviour and derived classes.
            { "UnityEngine.Behaviour", "enabled: {enabled}, gameObject: {name}" }
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
        public IExpressionEvaluators<TValue> ExpressionEvaluators { get; protected internal set; } = null!;

        public override int Priority => UnityRendererUtil.ValuePresenterPriority;

        public override bool IsApplicable(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType,
                                          IPresentationOptions options,
                                          IUserDataHolder dataHolder)
        {
            // Note that DebuggerDisplayObjectPresenter checks options.AllowTargetInvoke here
            return myUnityOptions.ExtensionsEnabled && options.AllowDebuggerDisplayEvaluation &&
                   TryCacheDebuggerDisplay(valueRole, instanceType.GetGenericTypeDefinition(), dataHolder);
        }

        // Return null to allow other providers a chance. If we throw EvaluatorException, it will be presented to the
        // user. OperationCancelledException will be logged and we move on to the next presenter. Any other exception
        // will leak
        public override IValuePresentation? PresentValue(IObjectValueRole<TValue> valueRole,
                                                         IMetadataTypeLite instanceType,
                                                         IPresentationOptions options,
                                                         IUserDataHolder dataHolder,
                                                         CancellationToken token)
        {
            var debuggerDisplayString = dataHolder.GetData(ourDebuggerDisplayStringKey);
            try
            {
                var valueReference = valueRole.ValueReference;
                var thisObj = valueReference.GetValue(options);
                var evaluationOptions =
                    valueReference.OriginatingFrame.DebuggerSession.Options.EvaluationOptions.Apply(options);

                // This can throw if there are members missing, which is entirely possible when debugging on a device,
                // due to stripping. It will throw EvaluatorException. Anything else is logged and thrown as a new
                // EvaluatorException. We can also get InvalidOperationException, but only if no other evaluators can
                // handle the current context, which is unlikely
                var display = ExpressionEvaluators.EvaluateDebuggerDisplay(valueReference.OriginatingFrame, thisObj, valueRole.Type,
                    debuggerDisplayString, evaluationOptions, token);

                var flags = valueReference.DefaultFlags;
                if (valueReference is CalculatedValueReferenceDecorator<TValue> { AllowDefaultTypePresentation: false })
                    flags |= ValueFlags.IsDefaultTypePresentation;

                // AggregatedPresentation will handle creating presentation parts from the list of value presentations,
                // but doesn't allow us to set Flags to hide the type name. Create the instance of the aggregated
                // presentation, then wrap the interface so we can override the Flags value
                var presentation = new AggregatedPresentation(display, options, instanceType);
                return new OverriddenFlagsValuePresentation(presentation, flags);
            }
            catch (Exception ex)
            {
                // Log as warning, not error - there's nothing the user can do, and we're likely to encounter this with
                // device builds
                myLogger.Warn(ex,
                    comment: $"Unable to evaluate debugger display string for type {instanceType.GetGenericTypeDefinition().FullName}: {debuggerDisplayString}. " +
                             "Expected behaviour on devices due to stripping");
                return null;
            }
        }

        private bool TryCacheDebuggerDisplay(IObjectValueRole<TValue> valueRole, IMetadataTypeLite instanceType,
                                             IUserDataHolder userDataHolder)
        {
            // If the (key) name of the reference is the same as its actual name, don't display the name in the value.
            // Replace with a second dictionary or whatever if we need to handle more types
            if (valueRole.ValueReference is CalculatedValueReferenceDecorator<TValue> { AllowNameInValue: false }
                && instanceType.FullName == "UnityEngine.GameObject")
            {
                userDataHolder.PutData(ourDebuggerDisplayStringKey, GameObjectDebuggerDisplayStringWithoutName);
                return true;
            }

            // DebuggerDisplayAttribute is inherited. This is important for applying a debug string on Behaviour, and
            // having it used in custom MonoBehaviour classes
            var current = instanceType;
            while (current != null)
            {
                if (myDebuggerDisplayValues.TryGetValue(current.GetGenericTypeDefinition().FullName,
                        out var displayString))
                {
                    userDataHolder.PutData(ourDebuggerDisplayStringKey, displayString);
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        private class OverriddenFlagsValuePresentation : IValuePresentation
        {
            private readonly IValuePresentation myValuePresentationImplementation;

            public OverriddenFlagsValuePresentation(IValuePresentation valuePresentationImplementation,
                                                    ValueFlags flags)
            {
                myValuePresentationImplementation = valuePresentationImplementation;
                Flags = flags;
            }

            public Refresh Refresh => myValuePresentationImplementation.Refresh;
            public ValueFlags Flags { get; }

            public ImmutableArray<ValuePresentationPart> Value => myValuePresentationImplementation.Value;
            public string DisplayValue => myValuePresentationImplementation.DisplayValue;
            public IMetadataTypeLite? Type => myValuePresentationImplementation.Type;
            public PresentationKind PresentationKind => myValuePresentationImplementation.PresentationKind;
            public StatisticsKind StatisticsKind => myValuePresentationImplementation.StatisticsKind;
            public object? PrimitiveValue => myValuePresentationImplementation.PrimitiveValue;
        }
    }
}
