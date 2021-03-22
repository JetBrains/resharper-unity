using System;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
{
    [ShellComponent]
    public class UnityCompiledElementOnlineHelpProvider : CompiledElementOnlineHelpProvider
    {
        private readonly ShowUnityHelp myShowUnityHelp;

        public UnityCompiledElementOnlineHelpProvider(ShowUnityHelp showUnityHelp)
        {
            myShowUnityHelp = showUnityHelp;
        }

        public override Uri GetUrl(ICompiledElement element)
        {
            if (!(element.Module is IAssemblyPsiModule module)) return null;
            if (!(element is ITypeElement || element is ITypeMember)) return null;

            var assemblyLocation = module.Assembly.Location;
            if (assemblyLocation == null || !assemblyLocation.ExistsFile)
                return null;

            if (!(assemblyLocation.Name.StartsWith("UnityEngine") || assemblyLocation.Name.StartsWith("UnityEditor")))
                return null;
            
            var searchableText = element.GetSearchableText();
            return searchableText == null
                ? null
                : myShowUnityHelp.GetUri(searchableText);
        }

        public override int Priority => 10;
        public override bool ShouldValidate => false;
    }
}