using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class KnownTypes
    {
        public static readonly IClrTypeName MonoBehaviour = new ClrTypeName("UnityEngine.MonoBehaviour");
    }
}