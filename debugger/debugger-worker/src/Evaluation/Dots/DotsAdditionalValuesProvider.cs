using System.Collections.Generic;
using System.Linq;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation.Dots
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityDotsAdditionalValuesProvider : UnityDotsAdditionalValuesProvider<Value>
    {
        public UnityDotsAdditionalValuesProvider(IDebuggerSession session,
            IValueServicesFacade<Value> valueServices,
            ISessionCreationInfo creationInfo,
            IUnityOptions unityOptions,
            ILogger logger)
            : base(session, valueServices, creationInfo, unityOptions, logger)
        {
        }
    }

    public class UnityDotsAdditionalValuesProvider<TValue> : IAdditionalValuesProvider
        where TValue : class
    {
        private const string UnityEntitiesPackageName = "com.unity.entities";

        private readonly IDebuggerSession mySession;
        private readonly IValueServicesFacade<TValue> myValueServices;
        private readonly IUnityOptions myUnityOptions;
        private readonly ILogger myLogger;

        private readonly bool myHasEntityPackage;

        private static readonly HashSet<string> ourSupportedEntityProcessingTypes = new(new[]
        {
            "Unity.Entities.IJobEntity",
            "Unity.Entities.IJobChunk",
        });

        protected UnityDotsAdditionalValuesProvider(IDebuggerSession session,
            IValueServicesFacade<TValue> valueServices,
            ISessionCreationInfo creationInfo,
            IUnityOptions unityOptions,
            ILogger logger)
        {
            mySession = session;
            myValueServices = valueServices;
            myUnityOptions = unityOptions;
            myLogger = logger;

            if (creationInfo.StartInfo is UnityStartInfoBase unityStartInfo)
                myHasEntityPackage = unityStartInfo.Packages.Contains(UnityEntitiesPackageName);
            else
                myHasEntityPackage = false;
        }

        public IEnumerable<IValueEntity> GetAdditionalLocals(IStackFrame frame)
        {
            // Do nothing if the entity package is not in the project
            // Do nothing if "Allow property evaluations..." option is disabled.
            if (!myHasEntityPackage || !myUnityOptions.ExtensionsEnabled ||
                !mySession.EvaluationOptions.AllowTargetInvoke)
            {
                yield break;
            }

            var currentEntity = GetCurrentEntity(frame);
            if (currentEntity != null)
                yield return currentEntity.ToValue(myValueServices);
        }

        private IValueReference<TValue>? GetCurrentEntity(IStackFrame frame)
        {
            return myLogger.CatchEvaluatorException<TValue, IValueReference<TValue>?>(
                () => TryGetValueFromParentFrame(frame),
                exception =>
                    myLogger.LogThrownUnityException(exception, frame, myValueServices, mySession.EvaluationOptions));
        }

        private IValueReference<TValue>? TryGetValueFromParentFrame(IStackFrame frame)
        {
            var containingReifiedType = frame.GetContainingReifiedType();
            if (containingReifiedType == null || !containingReifiedType.MetadataType
                    .ImplementedInterfaces
                    .Any(t => ourSupportedEntityProcessingTypes.Contains(t.FullName)))
                return null;

            var callerFrame = frame.CallerFrame;

            if (callerFrame == null)
                return null;

            var localVariables2 = callerFrame.GetLocalVariables2(mySession.EvaluationOptions)
                .Concat(callerFrame.GetArguments2(mySession.EvaluationOptions));

            IValue<TValue>? chunkValue = null;
            IValue<TValue>? entityIndexInChunkValue = null;
            foreach (var value in localVariables2)
            {
                if (value.SimpleName.Equals("chunk"))
                    chunkValue = value as IValue<TValue>;
                else if (value.SimpleName.Equals("entityIndexInChunk") || value.SimpleName.Equals("entityIndex"))
                    entityIndexInChunkValue = value as IValue<TValue>;

                if (chunkValue != null && entityIndexInChunkValue != null)
                    break;
            }


            if (chunkValue == null || entityIndexInChunkValue == null)
                return null;

            var entityIndexInChunk = entityIndexInChunkValue.ValueReference.AsPrimitive(mySession.EvaluationOptions)
                .GetPrimitiveSafe<int>();

            if (entityIndexInChunk == null)
                return null;

            var valueEntities = chunkValue.GetChildren(mySession.EvaluationOptions);

            if (valueEntities == null)
                return null;

            IValue<TValue>? entitiesArray = null;
            foreach (var valueEntity in valueEntities)
            {
                if (!valueEntity.SimpleName.Equals("Entities")) continue;

                entitiesArray = valueEntity as IValue<TValue>;
                break;
            }

            if (entitiesArray == null)
                return null;

            var arrayValueRole = entitiesArray.ValueReference.AsArray(mySession.EvaluationOptions);
            var element = arrayValueRole.GetElement(entityIndexInChunk.Value);
            if (element == null)
                return null;

            return new SimpleValueReference<TValue>(element, arrayValueRole.ElementType, "Current Entity",
                ValueOriginKind.Property,
                ValueFlags.None | ValueFlags.IsDefaultTypePresentation | ValueFlags.IsReadOnly, frame,
                myValueServices.RoleFactory);
        }
    }
}