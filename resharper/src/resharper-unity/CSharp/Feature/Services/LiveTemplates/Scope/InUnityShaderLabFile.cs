using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    public class InUnityShaderLabFile : InAnyLanguageFile, IMainScopePoint
    {
        private static readonly Guid DefaultUID = new Guid("ED25967E-EAEA-47CC-AB3C-C549C5F3F378");
        private static readonly Guid QuickUID = new Guid("1149A991-197E-468A-90E0-07700A01FBD3");

        public override Guid GetDefaultUID() => DefaultUID;
        public override PsiLanguageType RelatedLanguage => ShaderLabLanguage.Instance;
        public override string PresentableShortName => "ShaderLab (Unity)";

        protected override IEnumerable<string> GetExtensions()
        {
            yield return ShaderLabProjectFileType.SHADERLAB_EXTENSION;
        }

        public override string ToString() => "ShaderLab (Unity)";

        public string QuickListTitle => "Unity files";
        public Guid QuickListUID => QuickUID;
    }
}