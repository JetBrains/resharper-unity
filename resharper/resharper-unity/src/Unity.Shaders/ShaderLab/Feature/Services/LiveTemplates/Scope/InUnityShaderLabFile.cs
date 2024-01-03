#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Scope
{
    public class InUnityShaderLabFile : InAnyLanguageFile, IMainScopePoint
    {
        private static readonly Guid ourDefaultUID = new("ED25967E-EAEA-47CC-AB3C-C549C5F3F378");
        private static readonly Guid ourQuickUID = new("1149A991-197E-468A-90E0-07700A01FBD3");

        public override Guid GetDefaultUID() => ourDefaultUID;
        public override PsiLanguageType? RelatedLanguage => ShaderLabLanguage.Instance;
        public override string PresentableShortName => Strings.InUnityShaderLabFile_PresentableShortName_ShaderLab__Unity_;

        protected override IEnumerable<string> GetExtensions() => ShaderLabProjectFileType.Instance?.Extensions ?? EmptyList<string>.Enumerable;

        public override string ToString() => Strings.InUnityShaderLabFile_PresentableShortName_ShaderLab__Unity_;

        public string QuickListTitle => Strings.InUnityShaderLabFile_QuickListTitle_Unity_files;
        public Guid QuickListUID => ourQuickUID;
    }
}