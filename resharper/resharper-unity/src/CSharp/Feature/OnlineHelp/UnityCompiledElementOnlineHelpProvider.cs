using System;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;

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
            if (!element.IsBuiltInUnityClass()) return null;
            var searchableText = element.GetSearchableText();
            return searchableText == null
                ? null
                : myShowUnityHelp.GetUri(searchableText);
        }

        public override int Priority => 10;
        public override bool ShouldValidate => false;
    }
}