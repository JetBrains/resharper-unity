using System.Collections.Generic;
using System.Threading;
using JetBrains.Debugger.Worker.Plugins.Unity.Values.ValueReferences;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.Render.ChildrenRenderers;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Values.Render.ChildrenRenderers
{
    // Adds a "Children" group to UnityEditor.SerializedObject to show child serialised properties. Does not replace the
    // default children renderer
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class SerializedObjectChildrenRenderer<TValue> : ChildrenRendererBase<TValue, IObjectValueRole<TValue>>
        where TValue : class
    {
        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        public SerializedObjectChildrenRenderer(IUnityOptions unityOptions, ILogger logger)
        {
            myUnityOptions = unityOptions;
            myLogger = logger;
        }

        public override int Priority => UnityRendererUtil.ChildrenRendererPriority;

        // Let's just add a single item. Our priority means we'll be called first, so our children will be added to the
        // top. We also add a group which is pushed to top or bottom anyway.
        public override bool IsExclusive => false;

        protected override bool IsApplicable(IObjectValueRole<TValue> role, IMetadataTypeLite type,
                                            IPresentationOptions options, IUserDataHolder dataHolder)
        {
            return myUnityOptions.ExtensionsEnabled && type.Is("UnityEditor.SerializedObject");
        }

        protected override IEnumerable<IValueEntity> GetChildren(IObjectValueRole<TValue> valueRole,
                                                                 IMetadataTypeLite instanceType,
                                                                 IPresentationOptions options,
                                                                 IUserDataHolder dataHolder,
                                                                 CancellationToken token)
        {
            return new[] {new ChildrenGroup(valueRole, ValueServices, myLogger)};
        }

        private class ChildrenGroup : ValueGroupBase
        {
            private readonly IObjectValueRole<TValue> mySerializedObjectRole;
            private readonly IValueServicesFacade<TValue> myValueServices;
            private readonly ILogger myLogger;

            public ChildrenGroup(IObjectValueRole<TValue> serializedObjectRole,
                                 IValueServicesFacade<TValue> valueServices,
                                 ILogger logger)
                : base("Serialized Properties")
            {
                mySerializedObjectRole = serializedObjectRole;
                myValueServices = valueServices;
                myLogger = logger;
            }

            public override IEnumerable<IValueEntity> GetChildren(IPresentationOptions options,
                                                                  CancellationToken token = new CancellationToken())
            {
                return myLogger.CatchEvaluatorException<TValue, IEnumerable<IValueEntity>>(
                           () => GetChildrenImpl(options),
                           exception => myLogger.LogThrownUnityException(exception,
                               mySerializedObjectRole.ValueReference.OriginatingFrame, myValueServices, options))
                       ?? EmptyList<IValueEntity>.Enumerable;
            }

            private IValueEntity[] GetChildrenImpl(IValueFetchOptions options)
            {
                if (!TryInvokeGetIterator(mySerializedObjectRole, options, out var serializedPropertyRole))
                    return EmptyArray<IValueEntity>.Instance;

                var name = serializedPropertyRole.GetInstancePropertyReference("name")
                    ?.AsStringSafe(options)?.GetString() ?? "Child";

                // Tell the value presenter to hide the name field, as we're using it for the key name. Also hide the
                // type presentation - of course it's a SerializedProperty
                return new IValueEntity[]
                {
                    new CalculatedValueReferenceDecorator<TValue>(serializedPropertyRole.ValueReference,
                        myValueServices.RoleFactory, name, false, false).ToValue(myValueServices)
                };

                // Technically, we should now repeatedly call Copy() and Next(false) until Next returns false so that we
                // show all child properties of the SerializedObject. But empirically, there is only one direct child of
                // SerializedObject, called "Base", with a depth of -1. We'll avoid the unnecessary method invocations,
                // unless it turns out to be an actual issue.
            }

            private bool TryInvokeGetIterator(IObjectValueRole<TValue> serializedObjectRole,
                                              IValueFetchOptions options,
                                              out IObjectValueRole<TValue> returnedSerializedPropertyRole)
            {
                returnedSerializedPropertyRole = null;

                var method = MetadataTypeLiteEx.LookupInstanceMethodSafe(serializedObjectRole.ReifiedType.MetadataType,
                    MethodSelectors.SerializedObject_GetIterator, false);
                if (method == null)
                {
                    myLogger.Warn("Cannot find GetIterator method on SerializedObject");
                    return false;
                }

                returnedSerializedPropertyRole = new SimpleValueReference<TValue>(
                        serializedObjectRole.CallInstanceMethod(method),
                        serializedObjectRole.ValueReference.OriginatingFrame, myValueServices.RoleFactory)
                    .AsObjectSafe(options);
                if (returnedSerializedPropertyRole == null)
                {
                    myLogger.Warn("Unable to invoke GetIterator");
                    return false;
                }

                return true;
            }
        }
    }
}