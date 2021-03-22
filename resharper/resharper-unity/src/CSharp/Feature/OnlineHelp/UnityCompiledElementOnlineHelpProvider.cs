using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
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
            if (!element.GetSolution().HasUnityReference()) return null;
            if (!(element.Module is IAssemblyPsiModule module)) return null;
            var assemblyLocation = module.Assembly.Location;
            if (assemblyLocation == null || !assemblyLocation.ExistsFile)
                return null;

            if (!(assemblyLocation.Name.StartsWith("UnityEngine") || assemblyLocation.Name.StartsWith("UnityEditor")))
                return null;

            var searchableText = GetSearchableText(element);
            if (searchableText == null) return null;

            return myShowUnityHelp.GetUri(searchableText);
        }
        
        [Pure, CanBeNull]
        private static string GetSearchableText(ICompiledElement compiledElement)
        {
            if (compiledElement is ITypeElement)
            {
                return compiledElement.ShortName;
            }
            return ShowUnityHelp.StripPrefix(compiledElement.GetSearchableText());
        }

        public override int Priority => 10;
        public override bool ShouldValidate => false;
    }
}