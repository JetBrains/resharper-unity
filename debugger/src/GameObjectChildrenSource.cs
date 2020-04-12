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
    public class GameObjectChildrenSource : SyntheticGroupObjectValueSourceBase
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<GameObjectComponentsSource>();

        private readonly Value myGameObject;

        public GameObjectChildrenSource(SoftEvaluationContext context, IDebuggerHierarchicalObject parentSource,
                                        Value gameObject)
            : base(context, parentSource, "Children", ourLogger)
        {
            myGameObject = gameObject;
        }

        protected override ObjectValue[] GetChildrenSafe(ObjectPath path, int index, int count, IEvaluationOptions options)
        {
            // Get the children of the current game object, as seen in the Hierarchy view in the Unity Editor
            // These are the children of the current game object's transform property
            var transform = GetMember(myGameObject, "transform");
            if (transform == null)
                return EmptyArray<ObjectValue>.Instance;

            var childCountValue = (PrimitiveValue) GetMember(transform, "childCount");
            if (childCountValue == null)
                return EmptyArray<ObjectValue>.Instance;

            var childCount = (int) childCountValue.Value;

            var objectValues = new List<ObjectValue>(childCount);
            for (var i = 0; i < childCount; i++)
            {
                var currentIndex = Adaptor.CreateValue(Context, i);
                var child = InvokeInstanceMethod(transform, "GetChild", currentIndex);
                if (child != null)
                {
                    var childGameObject = GetMember(child, "gameObject");
                    if (childGameObject != null)
                    {
                        var name = (GetMember(childGameObject, "name") as StringMirror)?.Value ?? "GameObject";
                        objectValues.Add(CreateObjectValue(name, childGameObject, options));
                    }
                }
            }

            return objectValues.ToArray();
        }
    }
}