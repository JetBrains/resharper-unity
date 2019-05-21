using System;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    public class IsAvailableForMethod : InAnyFile, IMandatoryScopePoint
    {
        private static readonly Guid ourDefaultGuid = new Guid("71597959-A57B-4094-A68F-D1C05403D2E0");

        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => "Is available for method";
        public override string ToString() => "Is available for method";
    }
}