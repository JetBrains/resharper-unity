using System;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    public class UnityDotsScope : InUnityCSharpProject
    {
        private static readonly Guid DefaultUID = new("3C1BBCD2-1CE0-4C44-8677-D01294BEE4B7");
        private static readonly Guid QuickUID = new("A735778F-6C8C-4D4B-8638-A0815F2665EC");

        public override Guid GetDefaultUID() => DefaultUID;
        public override string PresentableShortName => Strings.UnityDots_projects;
        public override PsiLanguageType RelatedLanguage => CSharpLanguage.Instance;
        public override string QuickListTitle => Strings.UnityDots_projects;
        public override Guid QuickListUID => QuickUID;
    }
}