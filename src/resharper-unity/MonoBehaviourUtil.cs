using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class MonoBehaviourUtil
    {
        [NotNull] public static readonly MonoBehaviourEvent[] Events =
        {
            new MonoBehaviourEvent("Awake"),
            new MonoBehaviourEvent("FixedUpdate"),
            new MonoBehaviourEvent("LateUpdate"),
            new MonoBehaviourEvent("OnAnimatorIK",
                new MonoBehaviourEventParameter("layerIndex", PredefinedType.INT_FQN)),
            new MonoBehaviourEvent("OnAnimatorMove"),
            new MonoBehaviourEvent("OnApplicationFocus",
                new MonoBehaviourEventParameter("focusStatus", PredefinedType.BOOLEAN_FQN)),
            new MonoBehaviourEvent("OnApplicationPause",
                new MonoBehaviourEventParameter("pauseStatus", PredefinedType.BOOLEAN_FQN)),
            new MonoBehaviourEvent("OnApplicationQuit"),
            new MonoBehaviourEvent("OnAudioFilterRead",
                new MonoBehaviourEventParameter("data", PredefinedType.FLOAT_FQN, true),
                new MonoBehaviourEventParameter("channels", PredefinedType.INT_FQN)),
            new MonoBehaviourEvent("OnBecameInvisible"),
            new MonoBehaviourEvent("OnBecameVisible"),
            new MonoBehaviourEvent("OnCollisionEnter",
                new MonoBehaviourEventParameter("collision", UnityEnginePredefinedType.COLLISION_FQN)),
            new MonoBehaviourEvent("OnCollisionEnter2D",
                new MonoBehaviourEventParameter("collision", UnityEnginePredefinedType.COLLISION_2D_FQN)),
            new MonoBehaviourEvent("OnCollisionExit",
                new MonoBehaviourEventParameter("collision", UnityEnginePredefinedType.COLLISION_FQN)),
            new MonoBehaviourEvent("OnCollisionExit2D",
                new MonoBehaviourEventParameter("collision", UnityEnginePredefinedType.COLLISION_2D_FQN)),
            new MonoBehaviourEvent("OnCollisionStay",
                new MonoBehaviourEventParameter("collision", UnityEnginePredefinedType.COLLISION_FQN)),
            new MonoBehaviourEvent("OnCollisionStay2D",
                new MonoBehaviourEventParameter("collision", UnityEnginePredefinedType.COLLISION_2D_FQN)),
            new MonoBehaviourEvent("OnConnectedToServer"),
            new MonoBehaviourEvent("OnControllerColliderHit",
                new MonoBehaviourEventParameter("hit", UnityEnginePredefinedType.CONTROLLER_COLLIDER_HIT_FQN)),
            new MonoBehaviourEvent("OnDestroy"),
            new MonoBehaviourEvent("OnDisable"),
            new MonoBehaviourEvent("OnDisconnectedFromServer",
                new MonoBehaviourEventParameter("info", UnityEnginePredefinedType.NETWORK_DISCONNECTION_FQN)),
            new MonoBehaviourEvent("OnDrawGizmos"),
            new MonoBehaviourEvent("OnDrawGizmosSelected"),
            new MonoBehaviourEvent("OnEnable"),
            new MonoBehaviourEvent("OnFailedToConnect",
                new MonoBehaviourEventParameter("error", UnityEnginePredefinedType.NETWORK_CONNECTION_ERROR_FQN)),
            new MonoBehaviourEvent("OnFailedToConnectToMasterServer",
                new MonoBehaviourEventParameter("error", UnityEnginePredefinedType.NETWORK_CONNECTION_ERROR_FQN)),
            new MonoBehaviourEvent("OnGUI"),
            new MonoBehaviourEvent("OnJointBreak",
                new MonoBehaviourEventParameter("breakForce", PredefinedType.FLOAT_FQN)),
            new MonoBehaviourEvent("OnLevelWasLoaded",
                new MonoBehaviourEventParameter("level", PredefinedType.INT_FQN)),
            new MonoBehaviourEvent("OnMasterServerEvent",
                new MonoBehaviourEventParameter("msEvent", UnityEnginePredefinedType.MASTER_SERVER_EVENT_FQN)),
            new MonoBehaviourEvent("OnMouseDown"),
            new MonoBehaviourEvent("OnMouseDrag"),
            new MonoBehaviourEvent("OnMouseEnter"),
            new MonoBehaviourEvent("OnMouseExit"),
            new MonoBehaviourEvent("OnMouseOver"),
            new MonoBehaviourEvent("OnMouseUp"),
            new MonoBehaviourEvent("OnMouseUpAsButton"),
            new MonoBehaviourEvent("OnNetworkInstantiate",
                new MonoBehaviourEventParameter("info", UnityEnginePredefinedType.NETWORK_MESSAGE_INFO_FQN)),
            new MonoBehaviourEvent("OnParticleCollision",
                new MonoBehaviourEventParameter("other", UnityEnginePredefinedType.GAME_OBJECT_FQN)),
            new MonoBehaviourEvent("OnPlayerConnected",
                new MonoBehaviourEventParameter("player", UnityEnginePredefinedType.NETWORK_PLAYER_FQN)),
            new MonoBehaviourEvent("OnPlayerDisconnected",
                new MonoBehaviourEventParameter("player", UnityEnginePredefinedType.NETWORK_PLAYER_FQN)),
            new MonoBehaviourEvent("OnPostRender"),
            new MonoBehaviourEvent("OnPreCull"),
            new MonoBehaviourEvent("OnPreRender"),
            new MonoBehaviourEvent("OnRenderImage",
                new MonoBehaviourEventParameter("src", UnityEnginePredefinedType.RENDER_TEXTURE_FQN),
                new MonoBehaviourEventParameter("dest", UnityEnginePredefinedType.RENDER_TEXTURE_FQN)),
            new MonoBehaviourEvent("OnRenderObject"),
            new MonoBehaviourEvent("OnSerializeNetworkView",
                new MonoBehaviourEventParameter("stream", UnityEnginePredefinedType.BIT_STREAM_FQN),
                new MonoBehaviourEventParameter("info", UnityEnginePredefinedType.NETWORK_MESSAGE_INFO_FQN)),
            new MonoBehaviourEvent("OnServerInitialized"),
            new MonoBehaviourEvent("OnTransformChildrenChanged"),
            new MonoBehaviourEvent("OnTransformParentChanged"),
            new MonoBehaviourEvent("OnTriggerEnter",
                new MonoBehaviourEventParameter("other", UnityEnginePredefinedType.COLLIDER_FQN)),
            new MonoBehaviourEvent("OnTriggerEnter2D",
                new MonoBehaviourEventParameter("other", UnityEnginePredefinedType.COLLIDER_2D_FQN)),
            new MonoBehaviourEvent("OnTriggerExit",
                new MonoBehaviourEventParameter("other", UnityEnginePredefinedType.COLLIDER_FQN)),
            new MonoBehaviourEvent("OnTriggerExit2D",
                new MonoBehaviourEventParameter("other", UnityEnginePredefinedType.COLLIDER_2D_FQN)),
            new MonoBehaviourEvent("OnTriggerStay",
                new MonoBehaviourEventParameter("other", UnityEnginePredefinedType.COLLIDER_FQN)),
            new MonoBehaviourEvent("OnTriggerStay2D",
                new MonoBehaviourEventParameter("other", UnityEnginePredefinedType.COLLIDER_2D_FQN)),
            new MonoBehaviourEvent("OnValidate"),
            new MonoBehaviourEvent("OnWillRenderObject"),
            new MonoBehaviourEvent("Reset"),
            new MonoBehaviourEvent("Start"),
            new MonoBehaviourEvent("Update")
        };

        [NotNull] public static readonly JetHashSet<string> EventNames = Events.Select(e => e.Name).ToHashSet();

        public static bool IsEventHandler(string shortName)
        {
            return EventNames.Contains(shortName);
        }

        public static readonly IClrTypeName MonoBehaviourName = new ClrTypeName("UnityEngine.MonoBehaviour");

        public static bool IsMonoBehaviourType([NotNull] ITypeElement typeElement, [NotNull] IPsiModule module)
        {
            // TODO: Should the module + resolve context be for Unity.Engine.dll?
            // Then we could create a single type and reuse it
            var monoBehaviour = TypeFactory.CreateTypeByCLRName(MonoBehaviourName, module).GetTypeElement();
            return typeElement.IsDescendantOf(monoBehaviour);
        }
    }
}