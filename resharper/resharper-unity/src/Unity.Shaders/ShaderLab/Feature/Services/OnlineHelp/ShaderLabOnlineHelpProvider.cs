#nullable enable

using System;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Help;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.OnlineHelp
{
    [ShellComponent]
    public class ShaderLabOnlineHelpProvider : IOnlineHelpProvider
    {
        private readonly UnityDocumentation myDocumentation;
        private readonly UnityDocumentationCatalog myShaderLabCatalog;

        public ShaderLabOnlineHelpProvider(UnityDocumentation documentation)
        {
            myDocumentation = documentation;
            myShaderLabCatalog = UnityDocumentationCatalog.Create("ShaderLab", "Manual", "SL-");
        }

        public Uri? GetUrl(IDeclaredElement element)
        {
            var unityVersion = element.GetSolution().GetComponent<IUnityVersion>();
            if (element.GetElementType() != ShaderLabDeclaredElementType.Command)
                return null;
            var keyword = element.ShortName;
            return myDocumentation.GetDocumentationUri(unityVersion, myShaderLabCatalog, new HybridCollection<string>(keyword), keyword);
        }

        public string GetPresentableName(IDeclaredElement element) => element.ShortName;

        public int Priority => 0;
        public bool ShouldValidate => false;
        public bool IsAvailable(IDeclaredElement element) => element.PresentationLanguage.Is<ShaderLabLanguage>();
    }
}
