using System;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    // Mandatory scope point. Must be anywhere inside a Unity type, e.g. MonoBehaviour, ScriptableObject, etc. Combine
    // with e.g. InCSharpTypeMember to get "Must be in Unity type" AND "In C# 2.0+ where type member declaration is
    // allowed"
    public class MustBeInUnityType : InAnyFile, IMandatoryScopePoint
    {
        private static readonly Guid ourDefaultGuid = new Guid("F1DF3B02-5D9F-49C5-9A8F-23439468B344");

        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => "Unity type members";
        public override string ToString() => "Unity type members";
    }
}