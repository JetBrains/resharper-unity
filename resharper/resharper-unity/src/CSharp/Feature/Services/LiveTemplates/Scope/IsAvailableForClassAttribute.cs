using System;
using JetBrains.ReSharper.LiveTemplates.CSharp.Context;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    public class IsAvailableForClassAttribute : InCSharpFile
    {
        private static readonly Guid ourDefaultGuid = new Guid("AE22FA37-D337-48D7-B570-1ADE437DE6E3");

        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => "Where attribute for class is allowed";
        public override string ToString() => "Where attribute for class is allowed";
    }
}