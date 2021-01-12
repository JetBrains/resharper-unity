using System;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences;
using JetBrains.Util;
using MetadataLite.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend;
using Mono.Debugging.Backend.Values.Render.ValuePresenters;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ValuePresenters
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class SerializedPropertyPresenter<TValue> : ValuePresenterBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;

        public SerializedPropertyPresenter(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
        }

        public override int Priority => UnityRendererUtil.ValuePresenterPriority;

        public override bool IsApplicable(IObjectValueRole<TValue> valueRole,
                                          IMetadataTypeLite instanceType,
                                          IPresentationOptions options,
                                          IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && instanceType.Is("UnityEditor.SerializedProperty");
        }

        public override IValuePresentation PresentValue(IObjectValueRole<TValue> valueRole,
                                                        IMetadataTypeLite instanceType,
                                                        IPresentationOptions options, IUserDataHolder dataHolder,
                                                        CancellationToken token)
        {
            var showName = (valueRole.ValueReference as CalculatedValueReferenceDecorator<TValue>)?.AllowNameInValue ?? true;
            var showTypeName = (valueRole.ValueReference as CalculatedValueReferenceDecorator<TValue>)
                ?.AllowDefaultTypePresentation ?? true;

            var nameValuePresentation = valueRole.GetInstancePropertyReference("name")?.ToValue(ValueServices)
                ?.GetValuePresentation(options);
            var propertyTypeReference = valueRole.GetInstancePropertyReference("propertyType");
            var propertyTypeValuePresentation = propertyTypeReference?.ToValue(ValueServices)?.GetValuePresentation(options);

            var propertyTypeEnumValueObject = propertyTypeReference?.AsObjectSafe(options)?.GetEnumValue(options);
            var propertyType = (SerializedPropertyKind) Enum.ToObject(typeof(SerializedPropertyKind),
                propertyTypeEnumValueObject ?? SerializedPropertyKind.Generic);

            int? arraySize = null;
            string arrayElementType = null;
            string genericType = null;
            if (propertyType == SerializedPropertyKind.Generic)
            {
                if (Util.TryEvaluatePrimitiveProperty(valueRole, "isArray", options, out bool isArray) && isArray)
                {
                    arraySize = valueRole.GetInstancePropertyReference("arraySize")?.AsPrimitiveSafe(options)
                        ?.GetPrimitive<int>();
                    arrayElementType =
                        valueRole.GetInstancePropertyReference("arrayElementType")?.AsStringSafe(options)?.GetString();
                }
                else if (Util.TryEvaluatePrimitiveProperty(valueRole, "isFixedBuffer", options,
                             out bool isFixedBuffer) &&
                         isFixedBuffer)
                {
                    arraySize = valueRole.GetInstancePropertyReference("fixedBufferSize")?.AsPrimitiveSafe(options)
                        ?.GetPrimitive<int>();
                    arrayElementType =
                        valueRole.GetInstancePropertyReference("arrayElementType")?.AsStringSafe(options)?.GetString();
                }
                else
                {
                    genericType = valueRole.GetInstancePropertyReference("type")?.AsStringSafe(options)?.GetString();
                }
            }

            var valuePresentation = GetValuePresentation(valueRole, propertyType, options, out var extraDetail);

            var parts = PresentationBuilder.New();
            parts.OpenBrace();

            if (showName && nameValuePresentation != null)
            {
                parts.Comment("name: ").Add(nameValuePresentation.Value.ToArray())
                    .Add(ValuePresentationPart.Space);
            }

            parts.Comment("propertyType: ");
            if (propertyType == SerializedPropertyKind.Generic && arraySize != null && arrayElementType != null)
                parts.Default($"{arrayElementType}[{arraySize}]");
            else if (genericType != null)
                parts.Default(genericType);
            else if (propertyTypeValuePresentation != null)
                parts.Add(propertyTypeValuePresentation.Value.ToArray());
            else
                parts.Comment("(Unknown)");

            if (valuePresentation != null)
            {
                parts.Add(ValuePresentationPart.Space)
                    .Comment("value: ").Add(valuePresentation.Value.ToArray());
                if (!string.IsNullOrEmpty(extraDetail))
                    parts.Add(ValuePresentationPart.Space).SpecialSymbol("(").Default(extraDetail).SpecialSymbol(")");
            }

            parts.ClosedBrace();

            // Hide the default type presentation if we've been asked to
            var flags = !showTypeName ? ValueFlags.IsDefaultTypePresentation : 0;
            return SimplePresentation.Create(parts.Result(), ValueResultKind.Success, ValueFlags.None | flags,
                instanceType);
        }

        [CanBeNull]
        private IValuePresentation GetValuePresentation(IObjectValueRole<TValue> serializedPropertyRole,
                                                        SerializedPropertyKind propertyType,
                                                        IPresentationOptions options,
                                                        out string extraDetail)
        {
            extraDetail = null;

            var valueProperty = GetValueFieldName(propertyType);
            var valueReference = valueProperty == null ? null : serializedPropertyRole.GetInstancePropertyReference(valueProperty);

            if (propertyType == SerializedPropertyKind.Enum)
                extraDetail = SerializedPropertyHelper.GetEnumValueIndexAsEnumName(serializedPropertyRole, valueReference, options);
            else if (propertyType == SerializedPropertyKind.Character)
                extraDetail = SerializedPropertyHelper.GetIntValueAsPrintableChar(valueReference, options);
            else if (propertyType == SerializedPropertyKind.Integer)
            {
                var type = serializedPropertyRole.GetInstancePropertyReference("type")?.AsStringSafe(options)
                    ?.GetString();
                if (type == "char")
                    extraDetail = SerializedPropertyHelper.GetIntValueAsPrintableChar(valueReference, options);
            }

            return valueReference?.ToValue(ValueServices)?.GetValuePresentation(options);
        }

        [CanBeNull]
        private static string GetValueFieldName(SerializedPropertyKind propertyType)
        {
            switch (propertyType)
            {
                case SerializedPropertyKind.Integer: return "longValue";
                case SerializedPropertyKind.Boolean: return "boolValue";
                case SerializedPropertyKind.Float: return "doubleValue";
                case SerializedPropertyKind.String: return "stringValue";
                case SerializedPropertyKind.Color: return "colorValue";
                case SerializedPropertyKind.ObjectReference: return "objectReferenceValue";
                case SerializedPropertyKind.LayerMask: return "intValue";
                case SerializedPropertyKind.Enum: return "enumValueIndex";
                case SerializedPropertyKind.Vector2: return "vector2Value";
                case SerializedPropertyKind.Vector3: return "vector3Value";
                case SerializedPropertyKind.Vector4: return "vector4Value";
                case SerializedPropertyKind.Rect: return "rectValue";
                case SerializedPropertyKind.ArraySize: return "intValue";
                case SerializedPropertyKind.Character: return "intValue";
                case SerializedPropertyKind.AnimationCurve: return "animationCurveValue";
                case SerializedPropertyKind.Bounds: return "boundsValue";
                // Gradient doesn't have a compact value presenter. It just shows "GradientValue" which is not useful
                // case SerializedPropertyKind.Gradient: return "gradientValue";   // NOTE: Internal property
                case SerializedPropertyKind.Quaternion: return "quaternionValue";
                case SerializedPropertyKind.ExposedReference: return "exposedReferenceValue";
                case SerializedPropertyKind.FixedBufferSize: return "intValue";
                case SerializedPropertyKind.Vector2Int: return "vector2IntValue";
                case SerializedPropertyKind.Vector3Int: return "vector3IntValue";
                case SerializedPropertyKind.RectInt: return "rectIntValue";
                case SerializedPropertyKind.BoundsInt: return "boundsIntValue";
            }
            // TODO: What to display for ManagedReference? managedReferenceValue is a setter only property
            return null;
        }
    }
}