using System;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Client.Providers;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityObjectValueChildrenProvider
        : IObjectValueChildrenProvider<SoftEvaluationContext, TypeMirror, Value>
    {
        private readonly ILogger myLogger = Logger.GetLogger<UnityObjectValueChildrenProvider>();

        public ObjectValue[] GetChildren(SoftEvaluationContext ctx, IDebuggerValueOwner<Value> objectSource,
                                         TypeMirror type, Value obj,
                                         int firstItemIndex, int count, bool dereferenceProxy)
        {
            var options = ctx.Options;
            if (!options.AllowTargetInvoke)
                return EmptyArray<ObjectValue>.Instance;

            if (IsExpectedType(type, "Unity.Entities.Entity"))
                return GetChildrenForEntity(ctx, objectSource, obj);

            return EmptyArray<ObjectValue>.Instance;
        }

        private static bool IsExpectedType(TypeMirror type, string expectedType)
        {
            // TODO: Why is this case insensitive?
            return type.FullName.Equals(expectedType, StringComparison.OrdinalIgnoreCase);
        }

        private Value GetValue(SoftEvaluationContext ctx, string expression)
        {
            try
            {
                return ctx.Session.DefaultEvaluator.Evaluate(ctx, expression).Value;
            }
            catch (Exception e)
            {
                myLogger.Warn(e, $"Failed to get {expression} instance");
                return null;
            }
        }

        private ObjectValue[] GetChildrenForEntity(SoftEvaluationContext ctx, IDebuggerValueOwner<Value> objectSource,
                                                   Value entity)
        {
            var entityManager = GetValue(ctx,
                "global::Unity.Entities.World.Active.GetExistingManager<Unity.Entities.EntityManager>()");
            if (entityManager == null)
                return EmptyArray<ObjectValue>.Instance;

            var objectValueSource = new EntityComponentDataSource(ctx, objectSource, entity, entityManager);
            objectValueSource.Connect();
            return InitialiseObjectValues(objectValueSource);
        }

        private static ObjectValue[] InitialiseObjectValues(IObjectValueSource objectValueSource)
        {
            var objectValue = ObjectValue.CreateObject(objectValueSource, new ObjectPath(objectValueSource.Name),
                typeName: string.Empty, value: string.Empty,
                ObjectValueFlags.Group | ObjectValueFlags.ReadOnly | ObjectValueFlags.NoRefresh, null);
            objectValue.ChildSelector = string.Empty;

            return new[] {objectValue};
        }
    }
}