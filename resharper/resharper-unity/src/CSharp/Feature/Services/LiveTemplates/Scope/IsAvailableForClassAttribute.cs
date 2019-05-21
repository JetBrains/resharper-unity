using System;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    public class IsAvailableForClassAttribute : InAnyFile, IMandatoryScopePoint
    {
        private static readonly Guid ourDefaultGuid = new Guid("AE22FA37-D337-48D7-B570-1ADE437DE6E3");

        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => "Is available for class attribute";
        public override string ToString() => "Is available for class attribute";
    }
}