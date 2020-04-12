using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Mono.Debugger.Soft;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public class EntityComponentDataSource : SyntheticGroupObjectValueSourceBase
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<EntityComponentDataSource>();

        private readonly Value myEntityObject;
        private readonly Value myEntityManagerObject;
        private readonly TypeMirror myEntityManagerType;

        public EntityComponentDataSource(SoftEvaluationContext context, IDebuggerHierarchicalObject parentSource,
                                         Value entityObject, Value entityManagerObject)
            : base(context, parentSource, "Component Data", ourLogger)
        {
            myEntityObject = entityObject;
            myEntityManagerObject = entityManagerObject;
            myEntityManagerType = entityManagerObject.Type;
        }

        protected override ObjectValue[] GetChildrenSafe(ObjectPath path, int index, int count,
                                                         IEvaluationOptions options)
        {
            var allocatorTempObject = Evaluate("global::Unity.Collections.Allocator.Temp");
            var componentDataType = GetType("Unity.Entities.IComponentData").NotNull();
            var sharedComponentDataType = GetType("Unity.Entities.ISharedComponentData").NotNull();

            var entityManagerGetComponentTypesMethod = myEntityManagerType.GetMethod("GetComponentTypes");
            var entityManagerGetSharedComponentDataMethod = myEntityManagerType.GetMethod("GetSharedComponentData");
            var entityManagerGetComponentDataMethod = myEntityManagerType.GetMethod("GetComponentData");

            var componentTypesArray = Invoke(entityManagerGetComponentTypesMethod, myEntityManagerType,
                myEntityManagerObject, myEntityObject, allocatorTempObject.Value).NotNull();
            var componentTypesArrayLength =
                (PrimitiveValue) Adaptor.GetMember(Context, null, componentTypesArray, "Length").Value;

            var numberOfComponentTypes = (int) componentTypesArrayLength.Value;

            var objectValues = new List<ObjectValue>();
            for (var currentIndex = 0; currentIndex < numberOfComponentTypes; currentIndex++)
            {
                try
                {
                    var currentIndexValue = Adaptor.CreateValue(Context, currentIndex);
                    var currentComponent = Adaptor
                        .GetIndexerReference(Context, componentTypesArray, new[] {currentIndexValue}).Value;
                    var currentComponentType = currentComponent.Type;

                    var getManagedTypeMethod = currentComponentType.GetMethod("GetManagedType");
                    var dataManagedType = Invoke(getManagedTypeMethod, currentComponentType, currentComponent);
                    var dataManagedTypeFullName =
                        ((StringMirror) Adaptor.GetMember(Context, null, dataManagedType, "FullName").Value).Value;
                    var dataManagedTypeShortName =
                        ((StringMirror) Adaptor.GetMember(Context, null, dataManagedType, "Name").Value).Value;
                    var dataType = GetType(dataManagedTypeFullName);

                    MethodMirror getComponentDataMethod;
                    if (componentDataType.IsAssignableFrom(dataType))
                        getComponentDataMethod = entityManagerGetComponentDataMethod;
                    else if (sharedComponentDataType.IsAssignableFrom(dataType))
                        getComponentDataMethod = entityManagerGetSharedComponentDataMethod;
                    else
                    {
                        ourLogger.Warn("Unknown type of component data: {0}", dataManagedTypeFullName);
                        continue;
                    }

                    var getComponentDataMethodWithTypeArgs =
                        getComponentDataMethod.MakeGenericMethod(new[] {dataType});
                    var result = Invoke(getComponentDataMethodWithTypeArgs, myEntityManagerType,
                        myEntityManagerObject, myEntityObject);
                    objectValues.Add(LiteralValueReference
                        .CreateTargetObjectLiteral(Adaptor, Context, dataManagedTypeShortName, result)
                        .CreateObjectValue(options));
                }
                catch (Exception e)
                {
                    ourLogger.Error(e, "Failed to fetch parameter {0} of entity {1}", currentIndex, myEntityObject);
                }
            }

            return objectValues.ToArray();
        }

        [CanBeNull]
        protected Value Invoke(MethodMirror method, TypeMirror type, Value instance, params Value[] parameters)
        {
            return Adaptor.Invocator.RuntimeInvoke(Context, method, type, instance, parameters).Result;
        }
    }
}