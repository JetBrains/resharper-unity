using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Scope
{
    [ScopeCategoryUIProvider(Priority = Priority)]
    public class UnityScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
        // Needs to be less than other priorities in R#'s built in ScopeCategoryUIProvider
        // to push it to the end of the list
        private const int Priority = -200;

        static UnityScopeCategoryUIProvider()
        {
            TemplateImage.Register("UnityShaderLab", PsiSymbolsThemedIcons.FileShader.Id);
        }

        public UnityScopeCategoryUIProvider()
            : base(LogoIcons.Unity.Id)
        {
            MainPoint = new InUnityShaderLabFile();
        }

        public override IEnumerable<ITemplateScopePoint> BuildAllPoints()
        {
            yield return new InUnityShaderLabFile();
        }

        public override string CategoryCaption => "ShaderLab (Unity)";

        public override string Present(ITemplateScopePoint point)
        {
            switch (point)
            {
                case InUnityShaderLabFile _:
                    return "In Unity ShaderLab file";
                default:
                    return base.Present(point);
            }
        }
    }
}