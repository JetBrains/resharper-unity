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
    // Replaces the default children renderer for UnityEditor.SerializedProperty. Filters out properties that are not
    // relevant to the property (e.g. longValue for a string property). Also adds a "Children" group for complex objects
    // and a group for array or fixed size buffer elements.
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class SerializedPropertyChildrenRenderer<TValue> : FilteredObjectChildrenRendererBase<TValue>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;
        private readonly OneToSetMap<SerializedPropertyKind, string> myPerTypeFieldNames;
        private readonly ISet<SerializedPropertyKind> myHandledPropertyTypes;
        private readonly ISet<string> myKnownFieldNames;

        public SerializedPropertyChildrenRenderer(IUnityOptions unityOptions)
        {
            myUnityOptions = unityOptions;
            myPerTypeFieldNames = GetPerTypeFieldNames();
            myHandledPropertyTypes = myPerTypeFieldNames.Keys.ToSet();
            myHandledPropertyTypes.Add(SerializedPropertyKind.Generic); // Special handling
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
                                                                 IUserDataHolder dataHolder,
                                                                 CancellationToken token)
        {
            // Keep an eye on iterators and enumeration: we need to eagerly evaluate GetChildrenImpl so we can catch any
            // exceptions. The return value of GetChildren is eagerly evaluated, so we're not changing any semantics
            return Logger.CatchEvaluatorException<TValue, IEnumerable<IValueEntity>>(
                       () => GetChildrenImpl(valueRole, instanceType, options, dataHolder, token).ToList(),
                       exception =>
                           Logger.LogThrownUnityException(exception, valueRole.ValueReference.OriginatingFrame,
                               ValueServices, options))
                   ?? base.GetChildren(valueRole, instanceType, options, dataHolder, token);
        }

        private IEnumerable<IValueEntity> GetChildrenImpl(IObjectValueRole<TValue> valueRole,
                                                          IMetadataTypeLite instanceType,
                                                          IPresentationOptions options,
                                                          IUserDataHolder dataHolder,
                                                          CancellationToken token)
        {
            var enumValueObject = valueRole.GetInstancePropertyReference("propertyType")
                ?.AsObjectSafe(options)?.GetEnumValue(options);
            var propertyType = (SerializedPropertyKind) Enum.ToObject(typeof(SerializedPropertyKind),
                enumValueObject ?? SerializedPropertyKind.Invalid);

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
            if (propertyType == SerializedPropertyKind.Generic || propertyType == SerializedPropertyKind.String)
            {
                if (!Util.TryEvaluatePrimitiveProperty(valueRole, "isArray", options, out isArray) || !isArray)
                    Util.TryEvaluatePrimitiveProperty(valueRole, "isFixedBuffer", options, out isFixedBuffer);
            }

            // We filter all non-public members, so make sure we don't show the group
            var effectiveOptions = options.WithOverridden(o => o.GroupPrivateMembers = false);

            var references = EnumerateChildren(valueRole, effectiveOptions, token);
            references = FilterChildren(references, propertySpecificFields, isArray, isFixedBuffer);
            references = DecorateChildren(valueRole, references, propertyType, options);
            references = SortChildren(references);
            foreach (var valueEntity in RenderChildren(valueRole, references, effectiveOptions, token))
                yield return valueEntity;

            if (!Util.TryEvaluatePrimitiveProperty(valueRole, "hasChildren", options, out bool hasChildren))
                Logger.Warn("Cannot evaluate hasChildren for serializedProperty");
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
                if (!isArray && !isFixedBuffer && propertyType != SerializedPropertyKind.String)
                    yield return new ChildrenGroup(valueRole, ValueServices, Logger);
            }

            if (isArray)
            {
                if (!Util.TryEvaluatePrimitiveProperty(valueRole, "arraySize", options, out int arraySize))
                    Logger.Warn("Cannot evaluate arraySize for serializedProperty");
                else if (arraySize > 0)
                {
                    yield return new ArrayElementsGroup(valueRole, arraySize,
                        MethodSelectors.SerializedProperty_GetArrayElementAtIndex, ValueServices, Logger);
                }
            }
            else if (isFixedBuffer)
            {
                if (!Util.TryEvaluatePrimitiveProperty(valueRole, "fixedBufferSize", options, out int fixedBufferSize))
                    Logger.Warn("Cannot evaluate fixedBufferSize for serializedProperty");
                else if (fixedBufferSize > 0)
                {
                    yield return new ArrayElementsGroup(valueRole, fixedBufferSize,
                        MethodSelectors.SerializedProperty_GetFixedBufferElementAtIndex, ValueServices, Logger);
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
                if (isArray && myPerTypeFieldNames[SerializedPropertyKind.ArrayModifier].Contains(r.DefaultName))
                    return true;
                if (isFixedBuffer && myPerTypeFieldNames[SerializedPropertyKind.FixedBufferModifier].Contains(r.DefaultName))
                    return true;

                // Exclude property specific fields for other property types
                if (myKnownFieldNames.Contains(r.DefaultName))
                    return false;

                // Exclude children flags. We'll show a "Children" group if hasChildren is true. Non-visible children
                // seems to be fields marked with [HiddenInInspector]
                return r.DefaultName != "hasChildren" && r.DefaultName != "hasVisibleChildren";
            });
        }

        private IEnumerable<IValueReference<TValue>> DecorateChildren(IObjectValueRole<TValue> serializedProperty,
                                                                      IEnumerable<IValueReference<TValue>> references,
                                                                      SerializedPropertyKind propertyType,
                                                                      IPresentationOptions options)
        {
            switch (propertyType)
            {
                case SerializedPropertyKind.Enum:
                    return DecorateEnumValue(serializedProperty, references, options);

                // I was expecting string arrays to have elements with propertyType == SpecializedPropertyType.Character
                // but they instead seem to be Integer with a type == "char"
                case SerializedPropertyKind.Character:
                case SerializedPropertyKind.Integer when serializedProperty.GetInstancePropertyReference("type")?.AsStringSafe(options)?.GetString() == "char":
                    return DecorateCharacterValue(references, options);
                default:
                    return references;
            }
        }

        private IEnumerable<IValueReference<TValue>> DecorateEnumValue(IObjectValueRole<TValue> serializedProperty,
                                                                       IEnumerable<IValueReference<TValue>> references,
                                                                       IPresentationOptions options)
        {
            foreach (var reference in references)
            {
                if (reference.DefaultName == "enumValueIndex")
                {
                    var extraDetail =
                        SerializedPropertyHelper.GetEnumValueIndexAsEnumName(serializedProperty, reference, options);
                    if (extraDetail != null)
                    {
                        yield return new ExtraDetailValueReferenceDecorator<TValue>(reference,
                            ValueServices.RoleFactory, extraDetail);
                        continue;
                    }
                }

                yield return reference;
            }
        }

        private IEnumerable<IValueReference<TValue>> DecorateCharacterValue(IEnumerable<IValueReference<TValue>> references,
                                                                            IValueFetchOptions options)
        {
            foreach (var reference in references)
            {
                if (reference.DefaultName == "intValue" || reference.DefaultName == "longValue")
                {
                    var extraDetail = SerializedPropertyHelper.GetIntValueAsPrintableChar(reference, options);
                    if (extraDetail != null)
                    {
                        yield return new ExtraDetailValueReferenceDecorator<TValue>(reference,
                            ValueServices.RoleFactory, extraDetail);
                        continue;
                    }
                }

                yield return reference;
            }
        }

        private static OneToSetMap<SerializedPropertyKind, string> GetPerTypeFieldNames()
        {
            return new OneToSetMap<SerializedPropertyKind, string>
            {
                // Simple values
                {SerializedPropertyKind.Integer, "intValue"},
                {SerializedPropertyKind.Integer, "longValue"},
                {SerializedPropertyKind.Boolean, "boolValue"},
                {SerializedPropertyKind.Float, "floatValue"},
                {SerializedPropertyKind.Float, "doubleValue"},
                {SerializedPropertyKind.String, "stringValue"},
                {SerializedPropertyKind.Color, "colorValue"},
                {SerializedPropertyKind.LayerMask, "intValue"},
                {SerializedPropertyKind.Vector2, "vector2Value"},
                {SerializedPropertyKind.Vector3, "vector3Value"},
                {SerializedPropertyKind.Vector4, "vector4Value"},
                {SerializedPropertyKind.Rect, "rectValue"},
                {SerializedPropertyKind.Character, "intValue"},
                {SerializedPropertyKind.AnimationCurve, "animationCurveValue"},
                {SerializedPropertyKind.Bounds, "boundsValue"},
                {SerializedPropertyKind.Gradient, "gradientValue"}, // Note this is an internal property
                {SerializedPropertyKind.Quaternion, "quaternionValue"},
                {SerializedPropertyKind.Vector2Int, "vector2IntValue"},
                {SerializedPropertyKind.Vector3Int, "vector3IntValue"},
                {SerializedPropertyKind.RectInt, "rectIntValue"},
                {SerializedPropertyKind.BoundsInt, "boundsIntValue"},

                // Complex values: Object references
                {SerializedPropertyKind.ObjectReference, "objectReferenceValue"},
                {SerializedPropertyKind.ObjectReference, "objectReferenceInstanceIDValue"},

                // Complex values: Enum
                {SerializedPropertyKind.Enum, "enumValueIndex"},
                {SerializedPropertyKind.Enum, "enumDisplayNames"},
                {SerializedPropertyKind.Enum, "enumLocalizedDisplayNames"},
                {SerializedPropertyKind.Enum, "enumNames"},

                // Complex values: Exposed references
                // TODO: This is resolved via the "exposedName" serialised property. Should we show this?
                // TODO: This can be resolved via a "defaultValue" serialised property. Should we show this?
                {SerializedPropertyKind.ExposedReference, "exposedReferenceValue"},
                {SerializedPropertyKind.ExposedReference, "objectReferenceValue"},

                // Complex values: Managed references
                // Related to SerializeReference
                // TODO: Figure out a test case
                {SerializedPropertyKind.ManagedReference, "managedReferenceValue"},
                {SerializedPropertyKind.ManagedReference, "managedReferenceFullTypename"},
                {SerializedPropertyKind.ManagedReference, "managedReferenceFieldTypename"},

                // Arrays (variable length) and fixed buffer lists are implemented with several serialized properties.
                // The first is the property you see in your C# object. The next is a sibling (not child, annoyingly)
                // called "Array" with propertyType Generic and type == "Array". This has children. The first is a
                // property called "size" with propertyType ArraySize or FixedBufferSize. Then follows "data" siblings
                // which hold the elements (with path "...data[0]", "...data[1]", etc.). These data elements are
                // accessed using helper functions on SerializedProperty (GetArrayElementAtIndex and
                // GetFixedBufferElementAtIndex). Array size is a property on the original SerializedProperty.
                // With iteration based on children and not siblings, we miss out on showing these implementation
                // elements for arrays and fixed buffers (and strings, which are surprisingly serialised as arrays)
                {SerializedPropertyKind.ArraySize, "intValue"},
                {SerializedPropertyKind.FixedBufferSize, "intValue"},

                // Not strictly SerializedPropertyTypes, but it simplifies the code
                {SerializedPropertyKind.ArrayModifier, "isArray"},
                {SerializedPropertyKind.ArrayModifier, "arraySize"},
                {SerializedPropertyKind.ArrayModifier, "arrayElementType"},
                {SerializedPropertyKind.FixedBufferModifier, "isFixedBuffer"},
                {SerializedPropertyKind.FixedBufferModifier, "fixedBufferSize"},
            };
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
                : base($"[0..{arraySize - 1}]")
            {
                mySerializedPropertyRole = serializedPropertyRole;
                myArraySize = arraySize;
                myGetMethodElementSelector = getMethodElementSelector;
                myValueServices = valueServices;
                myLogger = logger;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options, CancellationToken token = new CancellationToken())
            {
                // Keep an eye on iterators and enumeration: we need to eagerly evaluate GetChildrenImpl so we can catch
                // any exceptions. The return value of GetChildren is eagerly evaluated, so we're not changing any
                // semantics. But remember that chunked groups are lazily evaluated, and need try/catch
                return myLogger.CatchEvaluatorException<TValue, IEnumerable<IValueEntity>>(
                           () => GetChildrenImpl(options, token).ToList(),
                           exception => myLogger.LogThrownUnityException(exception,
                               mySerializedPropertyRole.ValueReference.OriginatingFrame, myValueServices, options))
                       ?? EmptyList<IValueEntity>.Enumerable;
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
                var name = $"[{index}]";
                try
                {
                    var frame = mySerializedPropertyRole.ValueReference.OriginatingFrame;
                    var indexValue = myValueServices.ValueFactory.CreatePrimitive(frame, options, index);
                    var childSerializedPropertyValue = collection.CallInstanceMethod(myGetElementMethod, indexValue);
                    var valueReference = new SimpleValueReference<TValue>(childSerializedPropertyValue,
                        mySerializedPropertyRole.ReifiedType.MetadataType, name, ValueOriginKind.ArrayElement,
                        ValueFlags.None | ValueFlags.IsReadOnly, frame, myValueServices.RoleFactory);

                    // Tell the value presenter to hide the name, because it's always "data" (DefaultName is the key name)
                    // Also hide the type presentation - they can only ever be SerializedProperty instances
                    return new CalculatedValueReferenceDecorator<TValue>(valueReference, myValueServices.RoleFactory,
                        valueReference.DefaultName, false, false).ToValue(myValueServices);
                }
                catch (Exception e)
                {
                    // We must always return a value, as we're effectively showing the contents of an array here. We're
                    // possibly also being evaluated lazily, thanks to chunked arrays, so can't rely on the caller
                    // catching exceptions.
                    myLogger.LogExceptionSilently(e);
                    return myValueServices.ValueRenderers.GetValueStubForException(e, name,
                               collection.ValueReference.OriginatingFrame) as IValue
                           ?? new ErrorValue(name, "Unable to retrieve child serialized property");
                }
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
                // Keep an eye on iterators and enumeration: we need to eagerly evaluate GetChildrenImpl so we can catch
                // any exceptions. The return value of GetChildren is eagerly evaluated, so we're not changing any
                // semantics. This group is not chunked
                return myLogger.CatchEvaluatorException<TValue, IEnumerable<IValueEntity>>(
                           () => GetChildrenImpl(options, token).ToList(),
                           exception => myLogger.LogThrownUnityException(exception,
                               mySerializedPropertyRole.ValueReference.OriginatingFrame, myValueServices, options))
                       ?? EmptyList<IValueEntity>.Enumerable;
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

                    // Tell the value presenter to hide the name field, because we're using it for the key name. Also
                    // hide the default presentation. Of course it's a SerializedProperty, it's a child of a
                    // SerializedProperty
                    yield return new CalculatedValueReferenceDecorator<TValue>(
                            copiedSerializedPropertyRole.ValueReference, myValueServices.RoleFactory, name, false,
                            false)
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
                    return false;
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