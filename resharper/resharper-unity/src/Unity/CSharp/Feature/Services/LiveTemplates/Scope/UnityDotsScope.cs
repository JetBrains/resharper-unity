using System;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    public class UnityDotsScope : InUnityCSharpProject
    {
        private static readonly Guid DefaultUID = new("3C1BBCD2-1CE0-4C44-8677-D01294BEE4B7");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => Strings.UnityDots_projects;
    }
}