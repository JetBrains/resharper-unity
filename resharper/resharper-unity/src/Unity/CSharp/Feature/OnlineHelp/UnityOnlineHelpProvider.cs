using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityOnlineHelpProvider : IOnlineHelpProvider
    {
        private readonly ShowUnityHelp myShowUnityHelp;

        public UnityOnlineHelpProvider(ShowUnityHelp showUnityHelp)
        {
            myShowUnityHelp = showUnityHelp;
        }

        public Uri GetUrl(IDeclaredElement element)
        {
            if (!IsAvailable(element)) return null;
            var unityApi = element.GetSolution().GetComponent<UnityApi>();
            var keyword = element.GetUnityEventFunctionName(unityApi);
            keyword = ShowUnityHelp.FormatDocumentationKeyword(keyword);
            if (keyword == null) return null;
            return myShowUnityHelp.GetUri(keyword);
        }
        
        public string GetPresentableName(IDeclaredElement element)
        {
            return element.ShortName;
        }

        public bool IsAvailable(IDeclaredElement element)
        {
            return element.IsFromUnityProject();
        }

        public int Priority => 20; // for now there are no other providers like this one
        public bool ShouldValidate => false;
    }
}