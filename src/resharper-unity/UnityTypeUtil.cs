using System.Collections.Generic;
using System.Linq;
using System;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity
{
	public class UnityType
	{
		public readonly IClrTypeName Name;
		public readonly UnityTypeEvent[] Events;
		public readonly JetHashSet<string> EventNames;

		public UnityType(string name, UnityTypeEvent[] events)
		{
			Name = new ClrTypeName(name);
			Events = events;
			EventNames = Events.Select(e => e.Name).ToHashSet();
		}

		public bool IsAncestorOf([NotNull] ITypeElement typeElement, [NotNull] IPsiModule module)
		{
			// TODO: Should the module + resolve context be for Unity.Engine.dll?
			// Then we could create a single type and reuse it
			var type = TypeFactory.CreateTypeByCLRName(Name, module).GetTypeElement();
			return typeElement.IsDescendantOf(type);
		}
	}

	public static class UnityTypeUtil
	{
		// Unity Engine

		public static readonly UnityType UnityObject;
		public static readonly UnityType ScriptableObject;
		public static readonly UnityType MonoBehaviour;

		// Unity Editor

		public static readonly UnityType AssetPostprocessor;
		public static readonly UnityType EditorWindow;
		public static readonly UnityType Editor;

		public static readonly UnityType[] UnityTypes;
		public static readonly ClrTypeName[] UnityReferencingAttributes;

		static UnityTypeUtil()
		{
			UnityObject = new UnityType("UnityEngine.Object", new UnityTypeEvent[] { });

			UnityTypeEvent[] scriptableObjectEvents =
			{
				new UnityTypeEvent("OnEnable"),
				new UnityTypeEvent("OnDisable"),
				new UnityTypeEvent("OnDestroy")
			};

			ScriptableObject = new UnityType("UnityEngine.ScriptableObject", scriptableObjectEvents);

			UnityTypeEvent[] monoBehaviourEvents =
			{
				new UnityTypeEvent("Awake"),
				new UnityTypeEvent("FixedUpdate"),
				new UnityTypeEvent("LateUpdate"),
				new UnityTypeEvent("OnAnimatorIK",
					new UnityTypeParameter("layerIndex", PredefinedType.INT_FQN)),
				new UnityTypeEvent("OnAnimatorMove"),
				new UnityTypeEvent("OnApplicationFocus",
					new UnityTypeParameter("focusStatus", PredefinedType.BOOLEAN_FQN)),
				new UnityTypeEvent("OnApplicationPause",
					new UnityTypeParameter("pauseStatus", PredefinedType.BOOLEAN_FQN)),
				new UnityTypeEvent("OnApplicationQuit"),
				new UnityTypeEvent("OnAudioFilterRead",
					new UnityTypeParameter("data", PredefinedType.FLOAT_FQN, true),
					new UnityTypeParameter("channels", PredefinedType.INT_FQN)),
				new UnityTypeEvent("OnBecameInvisible"),
				new UnityTypeEvent("OnBecameVisible"),
				new UnityTypeEvent("OnCollisionEnter",
					new UnityTypeParameter("collision", UnityEnginePredefinedType.COLLISION_FQN)),
				new UnityTypeEvent("OnCollisionEnter2D",
					new UnityTypeParameter("collision", UnityEnginePredefinedType.COLLISION_2D_FQN)),
				new UnityTypeEvent("OnCollisionExit",
					new UnityTypeParameter("collision", UnityEnginePredefinedType.COLLISION_FQN)),
				new UnityTypeEvent("OnCollisionExit2D",
					new UnityTypeParameter("collision", UnityEnginePredefinedType.COLLISION_2D_FQN)),
				new UnityTypeEvent("OnCollisionStay",
					new UnityTypeParameter("collision", UnityEnginePredefinedType.COLLISION_FQN)),
				new UnityTypeEvent("OnCollisionStay2D",
					new UnityTypeParameter("collision", UnityEnginePredefinedType.COLLISION_2D_FQN)),
				new UnityTypeEvent("OnConnectedToServer"),
				new UnityTypeEvent("OnControllerColliderHit",
					new UnityTypeParameter("hit", UnityEnginePredefinedType.CONTROLLER_COLLIDER_HIT_FQN)),
				new UnityTypeEvent("OnDestroy"),
				new UnityTypeEvent("OnDisable"),
				new UnityTypeEvent("OnDisconnectedFromServer",
					new UnityTypeParameter("info", UnityEnginePredefinedType.NETWORK_DISCONNECTION_FQN)),
				new UnityTypeEvent("OnDrawGizmos"),
				new UnityTypeEvent("OnDrawGizmosSelected"),
				new UnityTypeEvent("OnEnable"),
				new UnityTypeEvent("OnFailedToConnect",
					new UnityTypeParameter("error", UnityEnginePredefinedType.NETWORK_CONNECTION_ERROR_FQN)),
				new UnityTypeEvent("OnFailedToConnectToMasterServer",
					new UnityTypeParameter("error", UnityEnginePredefinedType.NETWORK_CONNECTION_ERROR_FQN)),
				new UnityTypeEvent("OnGUI"),
				new UnityTypeEvent("OnJointBreak",
					new UnityTypeParameter("breakForce", PredefinedType.FLOAT_FQN)),
				new UnityTypeEvent("OnLevelWasLoaded",
					new UnityTypeParameter("level", PredefinedType.INT_FQN)),
				new UnityTypeEvent("OnMasterServerEvent",
					new UnityTypeParameter("msEvent", UnityEnginePredefinedType.MASTER_SERVER_EVENT_FQN)),
				new UnityTypeEvent("OnMouseDown"),
				new UnityTypeEvent("OnMouseDrag"),
				new UnityTypeEvent("OnMouseEnter"),
				new UnityTypeEvent("OnMouseExit"),
				new UnityTypeEvent("OnMouseOver"),
				new UnityTypeEvent("OnMouseUp"),
				new UnityTypeEvent("OnMouseUpAsButton"),
				new UnityTypeEvent("OnNetworkInstantiate",
					new UnityTypeParameter("info", UnityEnginePredefinedType.NETWORK_MESSAGE_INFO_FQN)),
				new UnityTypeEvent("OnParticleCollision",
					new UnityTypeParameter("other", UnityEnginePredefinedType.GAME_OBJECT_FQN)),
				new UnityTypeEvent("OnPlayerConnected",
					new UnityTypeParameter("player", UnityEnginePredefinedType.NETWORK_PLAYER_FQN)),
				new UnityTypeEvent("OnPlayerDisconnected",
					new UnityTypeParameter("player", UnityEnginePredefinedType.NETWORK_PLAYER_FQN)),
				new UnityTypeEvent("OnPostRender"),
				new UnityTypeEvent("OnPreCull"),
				new UnityTypeEvent("OnPreRender"),
				new UnityTypeEvent("OnRenderImage",
					new UnityTypeParameter("src", UnityEnginePredefinedType.RENDER_TEXTURE_FQN),
					new UnityTypeParameter("dest", UnityEnginePredefinedType.RENDER_TEXTURE_FQN)),
				new UnityTypeEvent("OnRenderObject"),
				new UnityTypeEvent("OnSerializeNetworkView",
					new UnityTypeParameter("stream", UnityEnginePredefinedType.BIT_STREAM_FQN),
					new UnityTypeParameter("info", UnityEnginePredefinedType.NETWORK_MESSAGE_INFO_FQN)),
				new UnityTypeEvent("OnServerInitialized"),
				new UnityTypeEvent("OnTransformChildrenChanged"),
				new UnityTypeEvent("OnTransformParentChanged"),
				new UnityTypeEvent("OnTriggerEnter",
					new UnityTypeParameter("other", UnityEnginePredefinedType.COLLIDER_FQN)),
				new UnityTypeEvent("OnTriggerEnter2D",
					new UnityTypeParameter("other", UnityEnginePredefinedType.COLLIDER_2D_FQN)),
				new UnityTypeEvent("OnTriggerExit",
					new UnityTypeParameter("other", UnityEnginePredefinedType.COLLIDER_FQN)),
				new UnityTypeEvent("OnTriggerExit2D",
					new UnityTypeParameter("other", UnityEnginePredefinedType.COLLIDER_2D_FQN)),
				new UnityTypeEvent("OnTriggerStay",
					new UnityTypeParameter("other", UnityEnginePredefinedType.COLLIDER_FQN)),
				new UnityTypeEvent("OnTriggerStay2D",
					new UnityTypeParameter("other", UnityEnginePredefinedType.COLLIDER_2D_FQN)),
				new UnityTypeEvent("OnValidate"),
				new UnityTypeEvent("OnWillRenderObject"),
				new UnityTypeEvent("Reset"),
				new UnityTypeEvent("Start"),
				new UnityTypeEvent("Update")
			};

			MonoBehaviour = new UnityType("UnityEngine.MonoBehaviour", monoBehaviourEvents);

			UnityTypeEvent[] editorWindowEvents =
			{
				new UnityTypeEvent("OnFocus"),
				new UnityTypeEvent("OnGUI"),
				new UnityTypeEvent("OnHierarchyChange"),
				new UnityTypeEvent("OnInspectorUpdate"),
				new UnityTypeEvent("OnLostFocus"),
				new UnityTypeEvent("OnProjectChange"),
				new UnityTypeEvent("OnSelectionChange"),
				new UnityTypeEvent("OnUpdate"),
			};

			EditorWindow = new UnityType("UnityEditor.EditorWindow", editorWindowEvents);

			UnityTypeEvent[] editorEvents =
			{
				new UnityTypeEvent("OnSceneGUI"),
			};

			Editor = new UnityType("UnityEditor.Editor", editorEvents);

			UnityTypeEvent[] assetPostprocessorEvents =
			{
				new UnityTypeEvent("OnAssignMaterialModel",
					new UnityTypeParameter("material", UnityEnginePredefinedType.MATERIAL_FQN),
					new UnityTypeParameter("renderer", UnityEnginePredefinedType.RENDERER_FQN)),
				new UnityTypeEvent("OnPostprocessAllAssets",
					new UnityTypeParameter("importedAssets", PredefinedType.STRING_FQN, true),
					new UnityTypeParameter("deletedAssets", PredefinedType.STRING_FQN, true),
					new UnityTypeParameter("movedAssets", PredefinedType.STRING_FQN, true),
					new UnityTypeParameter("movedFromAssetPaths", PredefinedType.STRING_FQN, true)),
				new UnityTypeEvent("OnPostprocessAssetbundleNameChanged",
					new UnityTypeParameter("assetPath", PredefinedType.STRING_FQN),
					new UnityTypeParameter("previousAssetBundleName", PredefinedType.STRING_FQN),
					new UnityTypeParameter("newAssetBundleName", PredefinedType.STRING_FQN)),
				new UnityTypeEvent("OnPostprocessAudio",
					new UnityTypeParameter("audioClip", UnityEnginePredefinedType.AUDIO_CLIP_FQN)),
				new UnityTypeEvent("OnPostprocessGameObjectWithUserProperties",
					new UnityTypeParameter("go", UnityEnginePredefinedType.GAME_OBJECT_FQN),
					new UnityTypeParameter("propNames", PredefinedType.STRING_FQN, true),
					new UnityTypeParameter("values", PredefinedType.OBJECT_FQN, true)),
				new UnityTypeEvent("OnPostprocessModel",
					new UnityTypeParameter("go", UnityEnginePredefinedType.GAME_OBJECT_FQN)),
				new UnityTypeEvent("OnPostprocessSpeedTree",
					new UnityTypeParameter("go", UnityEnginePredefinedType.GAME_OBJECT_FQN)),
				new UnityTypeEvent("OnPostprocessSprites",
					new UnityTypeParameter("texture", UnityEnginePredefinedType.TEXTURE_2D_FQN),
					new UnityTypeParameter("sprites", UnityEnginePredefinedType.SPRITE_FQN, true)),
				new UnityTypeEvent("OnPostprocessTexture",
					new UnityTypeParameter("texture", UnityEnginePredefinedType.TEXTURE_2D_FQN)),
				new UnityTypeEvent("OnPreprocessAnimation"),
				new UnityTypeEvent("OnPreprocessAudio"),
				new UnityTypeEvent("OnPreprocessModel"),
				new UnityTypeEvent("OnPreprocessSpeedTree"),
				new UnityTypeEvent("OnPreprocessTexture"),
			};

			AssetPostprocessor = new UnityType("UnityEditor.AssetPostprocessor", assetPostprocessorEvents);

			UnityTypes = new UnityType[]
			{
				UnityObject,
				ScriptableObject,
				MonoBehaviour,
				EditorWindow,
				Editor,
				AssetPostprocessor
			};

			UnityReferencingAttributes = new ClrTypeName[]
			{
				new ClrTypeName("UnityEngine.RuntimeInitializeOnLoadMethodAttribute"),
				new ClrTypeName("UnityEditor.InitializeOnLoadMethodAttribute"),
				new ClrTypeName("UnityEditor.MenuItem")
			};
		}

		public static bool IsEventHandler(string shortName, [NotNull] ITypeElement typeElement, [NotNull] IPsiModule module)
		{
			if (!IsUnityType(typeElement, module))
				return false;

			foreach (var unityType in UnityTypes)
			{
				if (unityType.IsAncestorOf(typeElement, module) && unityType.EventNames.Contains(shortName))
					return true;
			}

			return false;
		}

		public static bool IsReferencedByUnity(IMethod method)
		{
			if (method == null)
				return false;

			if (UnityReferencingAttributes.Any(x => method.HasAttributeInstance(x, true)))
				return true;

			var containingType = method.GetContainingType();

			if (containingType == null)
				return false;

			return IsEventHandler(method.ShortName, containingType, method.Module);
		}

		public static IEnumerable<UnityTypeEvent> FindMissingEvents([NotNull] JetHashSet<string> existingEventNames, [NotNull] ITypeElement typeElement, [NotNull] IPsiModule module)
		{
			List<UnityTypeEvent> missingEvents = new List<UnityTypeEvent>();

			foreach (var unityType in UnityTypes)
			{
				if (!unityType.IsAncestorOf(typeElement, module))
					continue;

				missingEvents.AddRange(unityType.Events.Where(e => !existingEventNames.Contains(e.Name)));
			}

			return missingEvents;
		}

		public static bool IsUnityType([NotNull] ITypeElement typeElement, [NotNull] IPsiModule module)
		{
			return UnityObject.IsAncestorOf(typeElement, module) || AssetPostprocessor.IsAncestorOf(typeElement, module);
		}

		// InitializeOnLoadMethod
		// RuntimeInitializeOnLoadMethod
		// MenuItem
	}
}