using System;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    // Mandatory scope point. Must be anywhere inside a type that can contain a serialised field, e.g. MonoBehaviour,
    // ScriptableObject, but also a normal class or struct that has the [Serializable] attribute
    public class MustBeInUnitySerializableType : InAnyFile, IMandatoryScopePoint
    {
        private static readonly Guid ourDefaultGuid = new Guid("38592678-C661-489F-BF1E-9FDF101F79DF");

        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => "Unity serializable type members";
        public override string ToString() => "Unity serializable type members";
    }
}