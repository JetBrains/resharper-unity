using System;
using System.Collections.Generic;
using System.Threading;
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
            {"UnityEngine.Quaternion", "({x}, {y}, {z}, {w})"},
            {"UnityEngine.Ray", "Origin: {m_Origin}, Dir: {m_Direction}"},
            {"UnityEngine.Ray2D", "Origin: {m_Origin}, Dir: {m_Direction}"},
            {"UnityEngine.Rect", "(x:{m_XMin}, y:{m_YMin}, width:{m_Width}, height:{m_Height})"},
            {"UnityEngine.RectOffset", "RectOffset (l:{left} r:{right} t:{top} b:{bottom})"},
            {"UnityEngine.Vector2", "({x}, {y})"},
            {"UnityEngine.Vector3", "({x}, {y}, {z})"},
            {"UnityEngine.Vector4", "({x}, {y}, {z}, {w})"},

            // Scene doesn't have any useful display details
            {"UnityEngine.SceneManagement.Scene", "{name} ({path})"}
        };

        public ExternalDebuggerDisplayObjectPresenter(ILogger logger)
        {
            myLogger = logger;
        }

        [Injected]
        public IExpressionEvaluators<TValue> ExpressionEvaluators { get; protected internal set; }

        // Make sure we have a higher priority than the default implementations so we're called first
        public override int Priority => 100;

        public override bool IsApplicable(IMetadataTypeLite instanceType, IPresentationOptions options,
                                          IUserDataHolder dataHolder)
        {
            // Note that DebuggerDisplayObjectPresenter checks options.AllowTargetInvoke here
            return options.AllowDebuggerDisplayEvaluation &&
                   myDebuggerDisplayValues.ContainsKey(instanceType.GetGenericTypeDefinition().FullName);
        }

        public override IValuePresentation PresentValue(IObjectValueRole<TValue> valueRole,
                                                        IMetadataTypeLite instanceType,
                                                        IPresentationOptions options, IUserDataHolder dataHolder,
                                                        CancellationToken token)
        {
            var type = instanceType.GetGenericTypeDefinition();
            if (myDebuggerDisplayValues.TryGetValue(type.FullName, out var debuggerDisplayString))
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
                        ValuePresentationPart.Default(DisplayStringUtil.EscapeString(displayString)), ValueFlags.None,
                        instanceType, displayString);
                }
                catch (Exception ex)
                {
                    myLogger.Error(ex,
                        $"Unable to evaluate debugger display string for type ${type.FullName}: ${debuggerDisplayString}");
                }
            }

            return SimplePresentation.EmptyPresentation;
        }
    }
}
