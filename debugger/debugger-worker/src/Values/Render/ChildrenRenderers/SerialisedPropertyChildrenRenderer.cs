using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.ValueReferences;
using JetBrains.Util;
using MetadataLite.API;
using MetadataLite.API.Selectors;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Values.Render.ChildrenRenderers
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class SerializedPropertyChildrenRenderer<TValue> : FilteredObjectChildrenRendererBase<TValue>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;
        private readonly OneToSetMap<PropertyKind, string> myPerTypeFieldNames;
        private readonly ISet<PropertyKind> myHandledPropertyTypes;
        private readonly ISet<string> myKnownFieldNames;

        public SerializedPropertyChildrenRenderer(IUnityOptions unityOptions, ILogger logger)
        {
            myUnityOptions = unityOptions;
            myLogger = logger;
            myPerTypeFieldNames = GetPerTypeFieldNames();
            myHandledPropertyTypes = myPerTypeFieldNames.Keys.ToSet();
            myHandledPropertyTypes.Add(PropertyKind.Generic);   // Special handling
            myKnownFieldNames = myPerTypeFieldNames.Values.ToSet();
        }

        public override int Priority => UnityRendererUtil.ChildrenRendererPriority;
        public override bool IsExclusive => true;

        protected override bool IsApplicable(IMetadataTypeLite type, IPresentationOptions options,
                                             IUserDataHolder dataHolder)
        {
            // Check debugger type proxy settings to avoid recursion while rendering Raw View. See GetChildren
            return myUnityOptions.ExtensionsEnabled
                   && options.EvaluateDebuggerTypeProxy
                   && type.Is("UnityEditor.SerializedProperty");
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder, CancellationToken token)
        {
            var enumValueObject = valueRole.GetInstancePropertyReference("propertyType")
                ?.AsObjectSafe(options)?.GetEnumValue(options);
            var propertyType = (PropertyKind) Enum.ToObject(typeof(PropertyKind),
                enumValueObject ?? PropertyKind.Invalid);

            // Fall back to showing everything if we don't have any special handling for the property type. This should
            // protect us if Unity introduces a new serialised property type.
            if (!myHandledPropertyTypes.Contains(propertyType))
            {
                foreach (var valueEntity in base.GetChildren(valueRole, instanceType, options, dataHolder, token))
                    yield return valueEntity;
                yield break;
            }

            var propertySpecificFields = myPerTypeFieldNames[propertyType];

            var isArray = false;
            var isFixedBuffer = false;

            // Generic means a custom serializable struct, an array or a fixed buffer (public unsafe fixed int buf[10])
            // Strings are also arrays, and have properties for each character. We'll show them too.
            if (propertyType == PropertyKind.Generic || propertyType == PropertyKind.String)
            {
                if (!Util.TryEvaluatePrimitiveProperty(valueRole, "isArray", options, out isArray) || !isArray)
                    Util.TryEvaluatePrimitiveProperty(valueRole, "isFixedBuffer", options, out isFixedBuffer);
            }

            // TODO: Add a DecorateChildren step so we can render character and enum values as something other than an integer

            // We filter all non-public members, so make sure we don't show the group
            var effectiveOptions = options.WithOverridden(o => o.GroupPrivateMembers = false);

            var references = EnumerateChildren(valueRole, effectiveOptions, token);
            references = FilterChildren(references, propertySpecificFields, isArray, isFixedBuffer);
            references = SortChildren(references);
            foreach (var valueEntity in RenderChildren(valueRole, references, effectiveOptions, token))
                yield return valueEntity;

            if (!Util.TryEvaluatePrimitiveProperty(valueRole, "hasChildren", options, out bool hasChildren))
                myLogger.Warn("Cannot evaluate hasChildren for serializedProperty");
            else if (hasChildren)
            {
                // Arrays, fixed buffer arrays and strings (which are arrays) all say they have children. They don't.
                // They have a special sibling (i.e. same depth) that can only be enumerated with Next(true), and is
                // skipped with Next(false).
                // We don't show these special siblings, because they're not children, and the data is already displayed
                // as part of the array/fixed buffer element group.
                // TODO: Should we show this?
                // It might be confusing if these special properties are never shown, especially if someone is using
                // the debugger to try to figure out the shape of the serialised stream. The question is, how do we show
                // something that is enumerated as a child, but stored as a sibling? The best solution is a dump of the
                // whole serialised stream, which is not what we're doing as part of the debugger. One solution would be
                // to add an "Array" node that is shown instead of "Children" - but that would still look like a child
                // node, and would only include the "Size" node over the existing array/fixed buffer element group.
                // For now, just ignore these special properties and leave it to the array/fixed buffer element group.
                if (!isArray && !isFixedBuffer && propertyType != PropertyKind.String)
                    yield return new ChildrenGroup(valueRole, ValueServices, myLogger);
            }

            if (isArray)
            {
                if (!Util.TryEvaluatePrimitiveProperty(valueRole, "arraySize", options, out int arraySize))
                    myLogger.Warn("Cannot evaluate arraySize for serializedProperty");
                else if (arraySize > 0)
                {
                    yield return new ArrayElementsGroup(valueRole, arraySize,
                        MethodSelectors.SerializedProperty_GetArrayElementAtIndex, ValueServices, myLogger);
                }
            }
            else if (isFixedBuffer)
            {
                if (!Util.TryEvaluatePrimitiveProperty(valueRole, "fixedBufferSize", options, out int fixedBufferSize))
                    myLogger.Warn("Cannot evaluate fixedBufferSize for serializedProperty");
                else if (fixedBufferSize > 0)
                {
                    yield return new ArrayElementsGroup(valueRole, fixedBufferSize,
                        MethodSelectors.SerializedProperty_GetFixedBufferElementAtIndex, ValueServices, myLogger);
                }
            }

            // Disable debugger type proxy options to avoid recursion. See IsApplicable.
            var rawViewOptions = options.WithOverridden(o => o.EvaluateDebuggerTypeProxy = false);
            yield return new SimpleEntityGroup(PresentationOptions.RawViewGroupName,
                valueRole.ValueReference.ToValue(ValueServices).GetChildren(rawViewOptions, token));
        }

        private IEnumerable<IValueReference<TValue>> FilterChildren(IEnumerable<IValueReference<TValue>> references,
                                                                    ISet<string> propertySpecificFields,
                                                                    bool isArray, bool isFixedBuffer)
        {
            return references.Where(r =>
            {
                // Include all fields for the current property type
                if (propertySpecificFields.Contains(r.DefaultName))
                    return true;

                // Ignore non-public fields. Look at the Raw View if you need more details
                if (!(ChildrenRenderingUtil.GetVisibilityOwner(r)?.IsPublic ?? true))
                    return false;

                // Show the array or fixed buffer fields if applicable
                if (isArray && myPerTypeFieldNames[PropertyKind.ArrayModifier].Contains(r.DefaultName))
                    return true;
                if (isFixedBuffer && myPerTypeFieldNames[PropertyKind.FixedBufferModifier].Contains(r.DefaultName))
                    return true;

                // Exclude property specific fields for other property types
                if (myKnownFieldNames.Contains(r.DefaultName))
                    return false;

                // Exclude children flags. We'll show a "Children" group if hasChildren is true. Non-visible children
                // seems to be fields marked with [HiddenInInspector]
                return r.DefaultName != "hasChildren" && r.DefaultName != "hasVisibleChildren";
            });
        }

        private static OneToSetMap<PropertyKind, string> GetPerTypeFieldNames()
        {
            return new OneToSetMap<PropertyKind, string>
            {
                // Simple values
                {PropertyKind.Integer, "intValue"},
                {PropertyKind.Integer, "longValue"},
                {PropertyKind.Boolean, "boolValue"},
                {PropertyKind.Float, "floatValue"},
                {PropertyKind.Float, "doubleValue"},
                {PropertyKind.String, "stringValue"},
                {PropertyKind.Color, "colorValue"},
                {PropertyKind.LayerMask, "intValue"},
                {PropertyKind.Vector2, "vector2Value"},
                {PropertyKind.Vector3, "vector3Value"},
                {PropertyKind.Vector4, "vector4Value"},
                {PropertyKind.Rect, "rectValue"},
                {PropertyKind.Character, "intValue"},
                {PropertyKind.AnimationCurve, "animationCurveValue"},
                {PropertyKind.Bounds, "boundsValue"},
                {PropertyKind.Gradient, "gradientValue"},
                {PropertyKind.Quaternion, "quaternionValue"},
                {PropertyKind.Vector2Int, "vector2IntValue"},
                {PropertyKind.Vector3Int, "vector3IntValue"},
                {PropertyKind.RectInt, "rectIntValue"},
                {PropertyKind.BoundsInt, "boundsIntValue"},

                // Complex values: Object references
                {PropertyKind.ObjectReference, "objectReferenceValue"},
                {PropertyKind.ObjectReference, "objectReferenceInstanceIDValue"},

                // Complex values: Enum
                {PropertyKind.Enum, "enumValueIndex"},
                {PropertyKind.Enum, "enumDisplayNames"},
                {PropertyKind.Enum, "enumLocalizedDisplayNames"},
                {PropertyKind.Enum, "enumNames"},

                // Complex values: Exposed references
                // TODO: This is resolved via the "exposedName" serialised property. Should we show this?
                // TODO: This can be resolved via a "defaultValue" serialised property. Should we show this?
                {PropertyKind.ExposedReference, "exposedReferenceValue"},
                {PropertyKind.ExposedReference, "objectReferenceValue"},

                // Complex values: Managed references
                // Related to SerializeReference
                // TODO: Figure out a test case
                {PropertyKind.ManagedReference, "managedReferenceValue"},
                {PropertyKind.ManagedReference, "managedReferenceFullTypename"},
                {PropertyKind.ManagedReference, "managedReferenceFieldTypename"},

                // Arrays (variable length) and fixed buffer lists are implemented with several serialized properties.
                // The first is the property you see in your C# object. The next is a sibling (not child, annoyingly)
                // called "Array" with propertyType Generic and type == "Array". This has children. The first is a
                // property called "size" with propertyType ArraySize or FixedBufferSize. Then follows "data" siblings
                // which hold the elements (with path "...data[0]", "...data[1]", etc.). These data elements are
                // accessed using helper functions on SerializedProperty (GetArrayElementAtIndex and
                // GetFixedBufferElementAtIndex). Array size is a property on the original SerializedProperty.
                // With iteration based on children and not siblings, we miss out on showing these implementation
                // elements for arrays and fixed buffers (and strings, which are surprisingly serialised as arrays)
                {PropertyKind.ArraySize, "intValue"},
                {PropertyKind.FixedBufferSize, "intValue"},

                // Not strictly SerializedPropertyTypes, but it simplifies the code
                {PropertyKind.ArrayModifier, "isArray"},
                {PropertyKind.ArrayModifier, "arraySize"},
                {PropertyKind.ArrayModifier, "arrayElementType"},
                {PropertyKind.FixedBufferModifier, "isFixedBuffer"},
                {PropertyKind.FixedBufferModifier, "fixedBufferSize"},
            };
        }

        // This MUST be kept in sync with UnityEditor.SerializedPropertyType
        internal enum PropertyKind
        {
            // Not used in SerializedPropertyType, but useful for code
            Invalid = -999,
            ArrayModifier = -99,
            FixedBufferModifier = -98,

            // Maps to UnityEditor.SerializedPropertyType
            Generic = -1, // Arrays, custom serializable structs, etc.
            Integer = 0,
            Boolean = 1,
            Float = 2,
            String = 3,
            Color = 4,
            ObjectReference = 5,
            LayerMask = 6,
            Enum = 7,
            Vector2 = 8,
            Vector3 = 9,
            Vector4 = 10,
            Rect = 11,
            ArraySize = 12, // Used when iterating through the properties that make up an array. Comes before the data
            Character = 13,
            AnimationCurve = 14,
            Bounds = 15,
            Gradient = 16,
            Quaternion = 17,
            ExposedReference = 18,
            FixedBufferSize = 19,   // Like ArraySize
            Vector2Int = 20,
            Vector3Int = 21,
            RectInt = 22,
            BoundsInt = 23,
            ManagedReference = 24
        }

        // Used for both variable length arrays and fixed buffer arrays
        private class ArrayElementsGroup : ChunkedValueGroupBase<IObjectValueRole<TValue>>
        {
            private readonly IObjectValueRole<TValue> mySerializedPropertyRole;
            private readonly int myArraySize;
            private readonly MethodSelector myGetMethodElementSelector;
            private readonly IValueServicesFacade<TValue> myValueServices;
            private readonly ILogger myLogger;
            private IMetadataMethodLite myGetElementMethod;

            public ArrayElementsGroup(IObjectValueRole<TValue> serializedPropertyRole, int arraySize,
                                      MethodSelector getMethodElementSelector,
                                      IValueServicesFacade<TValue> valueServices,
                                      ILogger logger)
                : base($"[0..{arraySize}]")
            {
                mySerializedPropertyRole = serializedPropertyRole;
                myArraySize = arraySize;
                myGetMethodElementSelector = getMethodElementSelector;
                myValueServices = valueServices;
                myLogger = logger;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                try
                {
                    return GetChildrenImpl(options, token);
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    return EmptyList<IValueEntity>.Enumerable;
                }
            }

            private IEnumerable<IValueEntity> GetChildrenImpl(IPresentationOptions options, CancellationToken token)
            {
                myGetElementMethod = MetadataTypeLiteEx.LookupInstanceMethodSafe(mySerializedPropertyRole.ReifiedType.MetadataType,
                        myGetMethodElementSelector, false);
                if (myGetElementMethod == null)
                {
                    myLogger.Warn("Cannot find GetArrayElementAtIndex/GetFixedBufferElementAtIndex method");
                    yield break;
                }

                if (options.ClusterArrays)
                {
                    foreach (var valueEntity in GetChunkedChildren(mySerializedPropertyRole, 0, myArraySize, options, token))
                        yield return valueEntity;
                }
                else
                {
                    for (var i = 0; i < myArraySize; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        yield return GetElementValueAt(mySerializedPropertyRole, i, options);
                    }
                }
            }

            protected override IValue GetElementValueAt(IObjectValueRole<TValue> collection, int index, IValueFetchOptions options)
            {
                var frame = mySerializedPropertyRole.ValueReference.OriginatingFrame;
                var indexValue = myValueServices.ValueFactory.CreatePrimitive(frame, options, index);
                var childSerializedPropertyValue = collection.CallInstanceMethod(myGetElementMethod, indexValue);
                return new SimpleValueReference<TValue>(childSerializedPropertyValue,
                        mySerializedPropertyRole.ReifiedType.MetadataType, $"[{index}]", ValueOriginKind.ArrayElement,
                        ValueFlags.None | ValueFlags.IsReadOnly, frame, myValueServices.RoleFactory)
                    .ToValue(myValueServices);
            }
        }

        private class ChildrenGroup : ValueGroupBase
        {
            private readonly IObjectValueRole<TValue> mySerializedPropertyRole;
            private readonly IValueServicesFacade<TValue> myValueServices;
            private readonly ILogger myLogger;

            public ChildrenGroup(IObjectValueRole<TValue> serializedPropertyRole,
                                 IValueServicesFacade<TValue> valueServices,
                                 ILogger logger)
                : base("Children")
            {
                mySerializedPropertyRole = serializedPropertyRole;
                myValueServices = valueServices;
                myLogger = logger;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options,
                                                                  CancellationToken token = new CancellationToken())
            {
                try
                {
                    return GetChildrenImpl(options, token);
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    return EmptyList<IValueEntity>.Enumerable;
                }
            }

            private IEnumerable<IValueEntity> GetChildrenImpl(IValueFetchOptions options, CancellationToken token)
            {
                // SerializedProperty is a view over a serialized stream. Calling Next() or GetEnumerator().MoveNext()
                // will update this view. We need to work with copies, so that the original value isn't updated
                if (!TryCopySerializedProperty(mySerializedPropertyRole, options, out var cursor))
                    yield break;

                // Get the depth of this "parent" property. We'll call Next() until  children's depth changes
                if (!Util.TryEvaluatePrimitiveProperty(cursor, "depth", options, out int initialDepth))
                {
                    myLogger.Warn("Unable to evaluate initial depth on serializedProperty");
                    yield break;
                }

                var nextMethod = MetadataTypeLiteEx.LookupInstanceMethodSafe(cursor.ReifiedType.MetadataType,
                    MethodSelectors.SerializedProperty_Next, false);
                if (nextMethod == null)
                {
                    myLogger.Warn("Cannot find Next method on SerializedProperty");
                    yield break;
                }

                var trueValue = myValueServices.ValueFactory.CreatePrimitive(
                    mySerializedPropertyRole.ValueReference.OriginatingFrame, options, true);
                var falseValue = myValueServices.ValueFactory.CreatePrimitive(
                    mySerializedPropertyRole.ValueReference.OriginatingFrame, options, false);

                // Call cursor.Next(true). Our cursor is now a view over the first child
                if (!TryInvokeNext(cursor, nextMethod, trueValue, options, out var nextResult))
                    yield break;

                var count = 0;
                while (nextResult)
                {
                    token.ThrowIfCancellationRequested();

                    if (!Util.TryEvaluatePrimitiveProperty(cursor, "depth", options, out int thisDepth))
                    {
                        myLogger.Warn("Unable to evaluate initial depth on serializedProperty");
                        yield break;
                    }

                    // SerializedProperties are a view on a stream of serialised objects (not the C# objects!). Children
                    // are simply the next node in the stream, but they have a depth set to current depth + 1. Iterating
                    // children has finished when the next node's depth is set back to current depth.
                    if (thisDepth != initialDepth + 1)
                        break;

                    // Yield a copy of the current instance
                    if (!TryCopySerializedProperty(cursor, options, out var copiedSerializedPropertyRole))
                    {
                        myLogger.Warn("Failed to copy current serialised property");
                        break;
                    }

                    var name = copiedSerializedPropertyRole.GetInstancePropertyReference("name")
                        ?.AsStringSafe(options)?.GetString() ?? $"prop{count}";
                    yield return new CalculatedValueReferenceDecorator<TValue>(
                            copiedSerializedPropertyRole.ValueReference, myValueServices.RoleFactory, name)
                        .ToValue(myValueServices);

                    // MoveNext(false). cursor is now viewing either the next child or a sibling of the original
                    // property, or is at the end of the stream (nextResult is false). If this evaluation fails,
                    // we've already logged and nextResult is already false, so we don't need extra error handling
                    TryInvokeNext(cursor, nextMethod, falseValue, options, out nextResult);

                    count++;
                }
            }

            [ContractAnnotation("false <= copiedSerializedPropertyRole:null")]
            private bool TryCopySerializedProperty(IObjectValueRole<TValue> serializedPropertyRole,
                                                   IValueFetchOptions options,
                                                   out IObjectValueRole<TValue> copiedSerializedPropertyRole)
            {
                copiedSerializedPropertyRole = null;

                // Get a copy of the property, so we can call Next(true) without updating the current instance
                var copyMethod = MetadataTypeLiteEx.LookupInstanceMethodSafe(
                    mySerializedPropertyRole.ReifiedType.MetadataType,
                    MethodSelectors.SerializedProperty_Copy, false);
                if (copyMethod == null)
                {
                    myLogger.Warn("Cannot find Copy method on SerializedProperty");
                    return true;
                }

                // CallInstanceMethod always returns not null (VoidValue if it fails)
                copiedSerializedPropertyRole = new SimpleValueReference<TValue>(
                        serializedPropertyRole.CallInstanceMethod(copyMethod),
                        mySerializedPropertyRole.ValueReference.OriginatingFrame, myValueServices.RoleFactory)
                    .AsObjectSafe(options);
                if (copiedSerializedPropertyRole == null)
                {
                    myLogger.Warn("Unable to Copy serializedProperty");
                    return false;
                }

                return true;
            }

            private bool TryInvokeNext(IObjectValueRole<TValue> serializedPropertyRole,
                                       IMetadataMethodLite nextMethod,
                                       TValue boolArg,
                                       IValueFetchOptions options,
                                       out bool returnValue)
            {
                returnValue = false;

                var returnValueRole = new SimpleValueReference<TValue>(
                        serializedPropertyRole.CallInstanceMethod(nextMethod, boolArg),
                        mySerializedPropertyRole.ValueReference.OriginatingFrame,
                        myValueServices.RoleFactory)
                    .AsPrimitiveSafe(options);
                if (returnValueRole == null)
                {
                    myLogger.Warn("Unable to call Next on serializedProperty");
                    return false;
                }

                returnValue = returnValueRole.GetPrimitive<bool>();
                return true;
            }
        }
    }
}