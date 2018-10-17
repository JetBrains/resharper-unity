using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class KnownTypes
    {
        // UnityEngine
        public static readonly IClrTypeName AddComponentMenu = new ClrTypeName("UnityEngine.AddComponentMenu");
        public static readonly IClrTypeName Animator = new ClrTypeName("UnityEngine.Animator");
        public static readonly IClrTypeName Camera = new ClrTypeName("UnityEngine.Camera");
        public static readonly IClrTypeName Component = new ClrTypeName("UnityEngine.Component");
        public static readonly IClrTypeName ExecuteInEditMode = new ClrTypeName("UnityEngine.ExecuteInEditMode");
        public static readonly IClrTypeName GameObject = new ClrTypeName("UnityEngine.GameObject");
        public static readonly IClrTypeName HideInInspector = new ClrTypeName("UnityEngine.HideInInspector");
        public static readonly IClrTypeName ImageEffectAfterScale = new ClrTypeName("UnityEngine.ImageEffectAfterScale");
        public static readonly IClrTypeName ImageEffectAllowedInSceneView = new ClrTypeName("UnityEngine.ImageEffectAllowedInSceneView");
        public static readonly IClrTypeName ImageEffectOpaque = new ClrTypeName("UnityEngine.ImageEffectOpaque");
        public static readonly IClrTypeName ImageEffectTransformsToLDR = new ClrTypeName("UnityEngine.ImageEffectTransformsToLDR");
        public static readonly IClrTypeName Material = new ClrTypeName("UnityEngine.Material");
        public static readonly IClrTypeName MonoBehaviour = new ClrTypeName("UnityEngine.MonoBehaviour");
        public static readonly IClrTypeName Object = new ClrTypeName("UnityEngine.Object");
        public static readonly IClrTypeName Physics = new ClrTypeName("UnityEngine.Physics");
        public static readonly IClrTypeName Physics2D = new ClrTypeName("UnityEngine.Physics2D");
        public static readonly IClrTypeName Resources = new ClrTypeName("UnityEngine.Resources");
        public static readonly IClrTypeName RuntimeInitializeOnLoadMethodAttribute = new ClrTypeName("UnityEngine.RuntimeInitializeOnLoadMethodAttribute");
        public static readonly IClrTypeName ScriptableObject = new ClrTypeName("UnityEngine.ScriptableObject");
        public static readonly IClrTypeName SerializeField = new ClrTypeName("UnityEngine.SerializeField");
        public static readonly IClrTypeName Shader = new ClrTypeName("UnityEngine.Shader");
        public static readonly IClrTypeName Transform = new ClrTypeName("UnityEngine.Transform");

        // UnityEngine.Networking
        public static readonly IClrTypeName NetworkBehaviour = new ClrTypeName("UnityEngine.Networking.NetworkBehaviour");
        public static readonly IClrTypeName SyncVarAttribute =
            new ClrTypeName("UnityEngine.Networking.SyncVarAttribute");

        // UnityEngine.Serialization
        public static readonly IClrTypeName FormerlySerializedAsAttribute =
            new ClrTypeName("UnityEngine.Serialization.FormerlySerializedAsAttribute");

        // UnityEditor
        public static readonly IClrTypeName CanEditMultipleObjects = new ClrTypeName("UnityEditor.CanEditMultipleObjects");
        public static readonly IClrTypeName CustomEditor = new ClrTypeName("UnityEditor.CustomEditor");
        public static readonly IClrTypeName DrawGizmo = new ClrTypeName("UnityEditor.DrawGizmo");
        public static readonly IClrTypeName GizmoType = new ClrTypeName("UnityEditor.GizmoType");
        public static readonly IClrTypeName InitializeOnLoadAttribute = new ClrTypeName("UnityEditor.InitializeOnLoadAttribute");
        public static readonly IClrTypeName InitializeOnLoadMethodAttribute = new ClrTypeName("UnityEditor.InitializeOnLoadMethodAttribute");

        public static readonly IClrTypeName DidReloadScripts = new ClrTypeName("UnityEditor.Callbacks.DidReloadScripts");
        public static readonly IClrTypeName OnOpenAssetAttribute = new ClrTypeName("UnityEditor.Callbacks.OnOpenAssetAttribute");
        public static readonly IClrTypeName PostProcessBuildAttribute = new ClrTypeName("UnityEditor.Callbacks.PostProcessBuildAttribute");
        public static readonly IClrTypeName PostProcessSceneAttribute = new ClrTypeName("UnityEditor.Callbacks.PostProcessSceneAttribute");
    }
}