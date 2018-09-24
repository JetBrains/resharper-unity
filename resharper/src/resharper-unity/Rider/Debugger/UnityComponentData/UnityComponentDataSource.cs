using System;
using System.Collections.Generic;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Mono.Debugger.Soft;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;
using Mono.Debugging.Soft.RuntimeInvocation;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.UnityComponentData
{
    public class UnityComponentDataSource : RemoteFrameObject, IObjectValueSource<SoftEvaluationContext>
    {
        private readonly Value myEntityObject;
        private readonly Value myEntityManagerObject;
        private readonly SoftDebuggerAdaptor myAdaptor;
        private readonly ExpressionEvaluator<SoftEvaluationContext, TypeMirror, Value> myExpressionEvaluator;
        private readonly SoftRuntimeInvocator mySoftRuntimeInvocator;
        private readonly ILogger myLogger = Logger.GetLogger<UnityComponentDataSource>();
        private readonly TypeMirror myEntityManagerType;

        public UnityComponentDataSource(SoftEvaluationContext context, IDebuggerHierarchicalObject parentSource,
            Value entityObject, Value entityManagerObject)
        {
            myEntityObject = entityObject;
            myEntityManagerType = entityManagerObject.Type;
            myEntityManagerObject = entityManagerObject;
            Context = context;
            ParentSource = parentSource;
            var softDebuggerSession = context.Session;
            myAdaptor = softDebuggerSession.Adapter;
            myExpressionEvaluator = softDebuggerSession.DefaultEvaluator;
            mySoftRuntimeInvocator = myAdaptor.Invocator;
        }

        public IDebuggerHierarchicalObject ParentSource { get; }
        public string Name => "Component data";
        public SoftEvaluationContext Context { get; }

        public ObjectValue[] GetChildren(ObjectPath path, int index, int count, IEvaluationOptions options)
        {
            try
            {
                var allocatorTempObject =
                    myExpressionEvaluator.Evaluate(Context, "global::Unity.Collections.Allocator.Temp");
                var componentDataType = myAdaptor.GetType(Context, "Unity.Entities.IComponentData");
                var sharedComponentDataType = myAdaptor.GetType(Context, "Unity.Entities.ISharedComponentData");

                var entityManagerGetComponentTypesMethod = myEntityManagerType.GetMethod("GetComponentTypes");
                var entityManagerGetSharedComponentDataMethod = myEntityManagerType.GetMethod("GetSharedComponentData");
                var entityManagerGetComponentDataMethod = myEntityManagerType.GetMethod("GetComponentData");

                var componentTypesArray = mySoftRuntimeInvocator.RuntimeInvoke(Context,
                    entityManagerGetComponentTypesMethod, myEntityManagerType,
                    myEntityManagerObject, new[] {myEntityObject, allocatorTempObject.Value}).Result;
                var componentTypesArrayLength =
                    (PrimitiveValue) myAdaptor.GetMember(Context, null, componentTypesArray, "Length").Value;

                var numberOfComponentTypes = (int) componentTypesArrayLength.Value;

                var objectValues = new List<ObjectValue>();
                for (var currentIndex = 0; currentIndex < numberOfComponentTypes; currentIndex++)
                {
                    try
                    {
                        var currentIndexValue = myAdaptor.CreateValue(Context, currentIndex);
                        var currentComponent =
                            myAdaptor.GetIndexerReference(Context, componentTypesArray, new[] {currentIndexValue})
                                .Value;
                        var currentComponentType = currentComponent.Type;

                        var getManagedTypeMethod = currentComponentType.GetMethod("GetManagedType");
                        var dataManagedType = mySoftRuntimeInvocator.RuntimeInvoke(Context, getManagedTypeMethod,
                            currentComponentType,
                            currentComponent, new Value[0]).Result;
                        var dataManagedTypeFullName =
                            ((StringMirror) myAdaptor.GetMember(Context, null, dataManagedType, "FullName").Value)
                            .Value;
                        var dataManagedTypeShortName =
                            ((StringMirror) myAdaptor.GetMember(Context, null, dataManagedType, "Name").Value)
                            .Value;
                        var dataType = myAdaptor.GetType(Context, dataManagedTypeFullName);

                        MethodMirror getComponentDataMethod;
                        if (componentDataType.IsAssignableFrom(dataType))
                            getComponentDataMethod = entityManagerGetComponentDataMethod;
                        else if (sharedComponentDataType.IsAssignableFrom(dataType))
                            getComponentDataMethod = entityManagerGetSharedComponentDataMethod;
                        else
                        {
                            myLogger.Warn("Unknown type of component data: {0}", dataManagedTypeFullName);
                            continue;
                        }

                        var getComponentDataMethodWithTypeArgs =
                            getComponentDataMethod.MakeGenericMethod(new[] {dataType});
                        var result = mySoftRuntimeInvocator.RuntimeInvoke(Context, getComponentDataMethodWithTypeArgs,
                            myEntityManagerType,
                            myEntityManagerObject, new[] {myEntityObject}).Result;
                        objectValues.Add(LiteralValueReference
                            .CreateTargetObjectLiteral(myAdaptor, Context, dataManagedTypeShortName, result)
                            .CreateObjectValue(options));
                    }
                    catch (Exception e)
                    {
                        myLogger.Error(e, "Failed to fetch parameter {0} of entity {1}", currentIndex, myEntityObject);
                    }
                }

                return objectValues.ToArray();
            }
            catch (Exception e)
            {
                myLogger.Error(e);
            }

            return new ObjectValue[0];
        }

        public ObjectValue GetValue(ObjectPath path, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }

        public IRawValue GetRawValue(ObjectPath path, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }

        public ValuePresentation SetValue(ObjectPath path, string value, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }

        public void SetRawValue(ObjectPath path, IRawValue value, IEvaluationOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
