using System.Collections.Generic;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Mono.Debugger.Soft;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    // Note that this only for works for Unity 5.3.2 and above, which is when multi-scene support was first introduced
    // Scene was only introduced in 5.3 and Scene.GetRootGameObjects only came in 5.3.2
    public class SceneRootObjectsSource : SyntheticGroupObjectValueSourceBase
    {
        private readonly Value myScene;
        private static readonly ILogger ourLogger = Logger.GetLogger<GameObjectComponentsSource>();

        public SceneRootObjectsSource(SoftEvaluationContext context, IDebuggerHierarchicalObject parentSource,
                                      Value scene)
            : base(context, parentSource, "Game Objects", ourLogger)
        {
            myScene = scene;
        }

        protected override ObjectValue[] GetChildrenSafe(ObjectPath path, int index, int count, IEvaluationOptions options)
        {
            // Calls scene.GetRootGameObjects (will only be available in Unity 5.3.2 and above)
            var gameObjects = InvokeInstanceMethod(myScene, "GetRootGameObjects") as ArrayMirror;
            if (gameObjects == null)
                return EmptyArray<ObjectValue>.Instance;

            var objectValues = new List<ObjectValue>(gameObjects.Length);
            foreach (Value gameObject in gameObjects)
            {
                var name = (GetMember(gameObject, "name") as StringMirror)?.Value ?? "GameObject";
                objectValues.Add(CreateObjectValue(name, gameObject, options));
            }

            return objectValues.ToArray();
        }
    }
}