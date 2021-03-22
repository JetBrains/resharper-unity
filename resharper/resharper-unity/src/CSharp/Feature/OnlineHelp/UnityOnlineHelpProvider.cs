using System;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
{
    [ShellComponent]
    public class UnityOnlineHelpProvider : IOnlineHelpProvider
    {
        private readonly ShowUnityHelp myShowUnityHelp;

        public UnityOnlineHelpProvider(ShowUnityHelp showUnityHelp)
        {
            myShowUnityHelp = showUnityHelp;
        }

        public Uri GetUrl(IDeclaredElement element)
        {
            var unityApi = element.GetSolution().GetComponent<UnityApi>();
            var name = element.GetUnityEventFunctionName(unityApi);
            if (!ShowUnityHelp.IsUnityKeyword(name)) return null;

            var keyword = ShowUnityHelp.StripPrefix(name);
            return myShowUnityHelp.GetUri(keyword);
        }

        public bool IsAvailable(IDeclaredElement element)
        {
            return element.IsFromUnityProject();
        }

        public int Priority => 20;
        public bool ShouldValidate => false;
    }
}