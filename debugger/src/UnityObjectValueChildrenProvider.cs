// using System;
// using JetBrains.Util;
// using JetBrains.Util.Logging;
// using Mono.Debugger.Soft;
// using Mono.Debugging.Autofac;
// using Mono.Debugging.Backend;
// using Mono.Debugging.Client;
// using Mono.Debugging.Client.Providers;
// using Mono.Debugging.Evaluation;
// using Mono.Debugging.Soft;
//
// namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
// {
//     [DebuggerSessionComponent(typeof(SoftDebuggerType))]
//     public class UnityObjectValueChildrenProvider
//         : IObjectValueChildrenProvider<SoftEvaluationContext, TypeMirror, Value>
//     {
//         private readonly ILogger myLogger = Logger.GetLogger<UnityObjectValueChildrenProvider>();
//
//         public ObjectValue[] GetChildren(SoftEvaluationContext ctx, IDebuggerValueOwner<Value> objectSource,
//                                          TypeMirror type, Value obj,
//                                          int firstItemIndex, int count, bool dereferenceProxy)
//         {
//             var options = ctx.Options;
//             if (!options.AllowTargetInvoke)
//                 return EmptyArray<ObjectValue>.Instance;
//
//             if (IsExpectedType(type, "UnityEngine.GameObject"))
//                 return GetChildrenForGameObject(ctx, objectSource, obj);
//             if (IsExpectedType(type, "UnityEngine.SceneManagement.Scene"))
//                 return GetChildrenForScene(ctx, objectSource, obj);
//             if (IsExpectedType(type, "Unity.Entities.Entity"))
//                 return GetChildrenForEntity(ctx, objectSource, obj);
//
//             return EmptyArray<ObjectValue>.Instance;
//         }
//
//         private static bool IsExpectedType(TypeMirror type, string expectedType)
//         {
//             // TODO: Why is this case insensitive?
//             return type.FullName.Equals(expectedType, StringComparison.OrdinalIgnoreCase);
//         }
//
//         private Value GetValue(SoftEvaluationContext ctx, string expression)
//         {
//             try
//             {
//                 return ctx.Session.Evaluators.Evaluate(ctx, expression).Value;
//             }
//             catch (Exception e)
//             {
//                 myLogger.Warn(e, ExceptionOrigin.Algorithmic, $"Failed to get {expression} instance");
//                 return null;
//             }
//         }
//
//         private ObjectValue[] GetChildrenForGameObject(SoftEvaluationContext ctx,
//                                                        IDebuggerValueOwner<Value> parentSource, Value gameObject)
//         {
//             var componentsSource = new GameObjectComponentsSource(ctx, parentSource, gameObject);
//             var componentsObjectValue = InitialiseObjectValues(componentsSource);
//
//             var childrenSource = new GameObjectChildrenSource(ctx, parentSource, gameObject);
//             var childrenObjectValue = InitialiseObjectValues(childrenSource);
//
//             return new[] {componentsObjectValue, childrenObjectValue};
//         }
//
//         private ObjectValue[] GetChildrenForScene(SoftEvaluationContext ctx, IDebuggerValueOwner<Value> parentSource,
//                                                   Value scene)
//         {
//             var rootObjectsSource = new SceneRootObjectsSource(ctx, parentSource, scene);
//             var rootObjectsValue = InitialiseObjectValues(rootObjectsSource);
//             return new[] {rootObjectsValue};
//         }
//
//         private ObjectValue[] GetChildrenForEntity(SoftEvaluationContext ctx, IDebuggerValueOwner<Value> parentSource,
//                                                    Value entity)
//         {
//             var entityManager = GetValue(ctx,
//                 "global::Unity.Entities.World.Active.GetExistingManager<Unity.Entities.EntityManager>()");
//             if (entityManager == null)
//                 return EmptyArray<ObjectValue>.Instance;
//
//             var objectValueSource = new EntityComponentDataSource(ctx, parentSource, entity, entityManager);
//             var objectValue = InitialiseObjectValues(objectValueSource);
//
//             return new[] {objectValue};
//         }
//
//         private static ObjectValue InitialiseObjectValues(IObjectValueSource objectValueSource)
//         {
//             // ReSharper disable ArgumentsStyleNamedExpression
//             // Displayed in the debugger as "objectValueSource.Name = {typeName} value"
//             var objectValue = ObjectValue.CreateObject(objectValueSource, new ObjectPath(objectValueSource.Name),
//                 TypePresentation.Null, value: string.Empty, 
//                 ObjectValueFlags.Group | ObjectValueFlags.ReadOnly | ObjectValueFlags.NoRefresh, null);
//             objectValue.ChildSelector = string.Empty;
//             return objectValue;
//         }
//     }
// }