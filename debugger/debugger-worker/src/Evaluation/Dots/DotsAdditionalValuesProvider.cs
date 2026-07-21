using System.Collections.Generic;
using System.Linq;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup;
using JetBrains.Lifetimes;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.Win32;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation.Dots
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class MonoUnityDotsAdditionalValuesProvider : UnityDotsAdditionalValuesProvider<Value>
    {
        public MonoUnityDotsAdditionalValuesProvider(IDebuggerSession session,
            IValueServicesFacade<Value> valueServices,
            ISessionCreationInfo creationInfo,
            IUnityOptions unityOptions,
            ILogger logger)
            : base(session, valueServices, creationInfo, unityOptions, logger)
        {
        }
    }

    [DebuggerSessionComponent(typeof(CorDebuggerType))]
    public class CorUnityDotsAdditionalValuesProvider : UnityDotsAdditionalValuesProvider<ICorValue>
    {
        public CorUnityDotsAdditionalValuesProvider(IDebuggerSession session,
            IValueServicesFacade<ICorValue> valueServices,
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

            if (creationInfo.StartInfo is UnityStartInfo unityStartInfo)
                myHasEntityPackage = unityStartInfo.GetProjectData().Packages.Contains(UnityEntitiesPackageName);
            else
                myHasEntityPackage = false;
        }

        public IEnumerable<IValueEntity> GetAdditionalLocals(IStackFrame frame, Lifetime lifetime)
        {
            // Do nothing if the entity package is not in the project
            // Do nothing if "Allow property evaluations..." option is disabled.
            if (!myHasEntityPackage || !myUnityOptions.ExtensionsEnabled ||
                !mySession.EvaluationOptions.AllowTargetInvoke)
            {
                yield break;
            }

            var currentEntity = GetCurrentEntity(frame, lifetime);
            if (currentEntity != null)
                yield return currentEntity.ToValue(myValueServices);
        }

        private IValueReference<TValue>? GetCurrentEntity(IStackFrame frame, Lifetime lifetime)
        {
            return myLogger.CatchEvaluatorException<TValue, IValueReference<TValue>?>(
                () => TryGetCurrentEntityFromParentFrame(frame, lifetime),
                exception =>
                    myLogger.LogThrownUnityException(exception, frame, myValueServices, mySession.EvaluationOptions));
        }

        private IValueReference<TValue>? TryGetCurrentEntityFromParentFrame(IStackFrame frame, Lifetime lifetime)
        {
            var containingReifiedType = frame.GetContainingReifiedType();
            if (containingReifiedType == null || !IsIJobEntityType(containingReifiedType))
                return null;

            var callerFrame = frame.CallerFrame;

            if (callerFrame == null)
                return null;

            var localVariables2 = callerFrame.GetLocalVariables2(lifetime, mySession.EvaluationOptions)
                .Concat(callerFrame.GetArguments2(lifetime, mySession.EvaluationOptions));

            IValue<TValue>? chunkValue = null;
            IValue<TValue>? entityIndexInChunkValue = null;
            foreach (var value in localVariables2)
            {
                lifetime.ThrowIfNotAlive();

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

            // in CoreCLR the refs are not automatically dereferenced, so we need to do it to get the access to actual children
            if (chunkValue.GetPrimaryRole(mySession.EvaluationOptions) is IPointerLikeValueRole<TValue> chunkPointerLikeValueRole)
            {
                chunkValue = chunkPointerLikeValueRole.UnderlyingValueReference.ToValue(myValueServices);
            }
            
            var valueEntities = chunkValue.GetChildren(mySession.EvaluationOptions);

            if (valueEntities == null)
                return null;

            IValue<TValue>? entitiesArray = null;
            foreach (var valueEntity in valueEntities)
            {
                lifetime.ThrowIfNotAlive();

                if (!valueEntity.SimpleName.Equals("Entities")) continue;

                entitiesArray = valueEntity as IValue<TValue>;
                break;
            }

            if (entitiesArray == null)
                return null;

            var arrayValueRole = entitiesArray.ValueReference.AsArray(mySession.EvaluationOptions);
            lifetime.ThrowIfNotAlive();

            var element = arrayValueRole.GetElement(entityIndexInChunk.Value);
            if (element == null)
                return null;

            return new SimpleValueReference<TValue>(element, arrayValueRole.ElementType, "Current Entity",
                ValueOriginKind.Property,
                ValueFlags.None | ValueFlags.IsTypeCanBeDerivedFromContext | ValueFlags.IsReadOnly, frame,
                myValueServices.RoleFactory);

            bool IsIJobEntityType(IReifiedType reifiedType)
            {
                const string iJobEntityTypeName = "Unity.Entities.IJobEntity";
                return reifiedType.MetadataType.ImplementedInterfaces
                    .Any(t => t.FullName == iJobEntityTypeName);
            }
        }
    }
}