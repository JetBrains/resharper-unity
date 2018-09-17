using System;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.Providers;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.UnityComponentData
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class
        UnityEntitiesObjectValueChildrenProvider : IObjectValueChildrenProvider<SoftEvaluationContext, TypeMirror, Value
        >
    {
        private readonly ILogger myLogger = Logger.GetLogger<UnityEntitiesObjectValueChildrenProvider>();

        public ObjectValue[] GetChildren(SoftEvaluationContext ctx, IDebuggerValueOwner<Value> objectSource,
            TypeMirror type, Value obj,
            int firstItemIndex, int count, bool dereferenceProxy)
        {
            var options = ctx.Options;
            if (!options.AllowTargetInvoke)
                return new ObjectValue[0];
            if (!type.FullName.Equals("Unity.Entities.Entity", StringComparison.OrdinalIgnoreCase))
                return new ObjectValue[0];
            Value entityManagerObject = null;
            try
            {
                entityManagerObject = ctx.Session.DefaultEvaluator.Evaluate(ctx,
                    "global::Unity.Entities.World.Active.GetExistingManager<Unity.Entities.EntityManager>()").Value;
            }
            catch (Exception e)
            {
                myLogger.Warn("Failed to get EntityManager instance", e);
                return new ObjectValue[0];
            }

            var unityComponentDataSource = new UnityComponentDataSource(ctx, objectSource, obj, entityManagerObject);
            unityComponentDataSource.Connect();
            var objectValue = ObjectValue.CreateObject(unityComponentDataSource,
                new ObjectPath(unityComponentDataSource.Name), "", "",
                ObjectValueFlags.Group | ObjectValueFlags.ReadOnly | ObjectValueFlags.NoRefresh, null);
            objectValue.ChildSelector = "";
            return new[] {objectValue};
        }
    }
}