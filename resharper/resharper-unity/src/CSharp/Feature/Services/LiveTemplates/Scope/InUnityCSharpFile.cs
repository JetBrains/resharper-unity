using System;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    public class InUnityCSharpFile : InAnyLanguageFile, IMandatoryScopePoint
    {
        private static readonly Guid ourDefaultGuid = new Guid("B65158D8-B83E-417C-B67E-66379E8F3CEB");

        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => "Unity type members";
        public override string ToString() => "Unity type members";
    }
}