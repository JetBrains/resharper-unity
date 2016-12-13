using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class KnownTypes
    {
        public static readonly IClrTypeName MonoBehaviour = new ClrTypeName("UnityEngine.MonoBehaviour");
        public static readonly IClrTypeName NonSerializedAttribute = new ClrTypeName("System.NonSerializedAttribute");
        public static readonly IClrTypeName SerializeField = new ClrTypeName("UnityEngine.SerializeField");

        public static readonly IClrTypeName InitializeOnLoadAttribute = new ClrTypeName("UnityEditor.InitializeOnLoadAttribute");
    }
}