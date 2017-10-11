using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class KnownTypes
    {
        // UnityEngine
        public static readonly IClrTypeName Component = new ClrTypeName("UnityEngine.Component");
        public static readonly IClrTypeName GameObject = new ClrTypeName("UnityEngine.GameObject");
        public static readonly IClrTypeName MonoBehaviour = new ClrTypeName("UnityEngine.MonoBehaviour");
        public static readonly IClrTypeName RuntimeInitializeOnLoadMethodAttribute = new ClrTypeName("UnityEngine.RuntimeInitializeOnLoadMethodAttribute");
        public static readonly IClrTypeName ScriptableObject = new ClrTypeName("UnityEngine.ScriptableObject");
        public static readonly IClrTypeName SerializeField = new ClrTypeName("UnityEngine.SerializeField");

        // UnityEngine.Networking
        public static readonly IClrTypeName NetworkBehaviour = new ClrTypeName("UnityEngine.Networking.NetworkBehaviour");
        public static readonly IClrTypeName SyncVarAttribute =
            new ClrTypeName("UnityEngine.Networking.SyncVarAttribute");

        // UnityEditor
        public static readonly IClrTypeName InitializeOnLoadAttribute = new ClrTypeName("UnityEditor.InitializeOnLoadAttribute");
        public static readonly IClrTypeName InitializeOnLoadMethodAttribute = new ClrTypeName("UnityEditor.InitializeOnLoadMethodAttribute");

        public static readonly IClrTypeName DidReloadScripts = new ClrTypeName("UnityEditor.Callbacks.DidReloadScripts");
        public static readonly IClrTypeName OnOpenAssetAttribute = new ClrTypeName("UnityEditor.Callbacks.OnOpenAssetAttribute");
        public static readonly IClrTypeName PostProcessBuildAttribute = new ClrTypeName("UnityEditor.Callbacks.PostProcessBuildAttribute");
        public static readonly IClrTypeName PostProcessSceneAttribute = new ClrTypeName("UnityEditor.Callbacks.PostProcessSceneAttribute");

        // System
        public static readonly IClrTypeName NonSerializedAttribute = new ClrTypeName("System.NonSerializedAttribute");
    }
}