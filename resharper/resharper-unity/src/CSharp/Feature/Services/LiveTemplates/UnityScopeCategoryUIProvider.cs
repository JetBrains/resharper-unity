using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates
{
    [ScopeCategoryUIProvider(Priority = Priority)]
    public class UnityScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
        // Needs to be less than other priorities in R#'s built in ScopeCategoryUIProvider
        // to push it to the end of the list
        private const int Priority = -200;

        public UnityScopeCategoryUIProvider()
            : base(LogoThemedIcons.UnityLogo.Id)
        {
            MainPoint = new InUnityShaderLabFile();
        }

        public override IEnumerable<ITemplateScopePoint> BuildAllPoints()
        {
            yield return new InUnityShaderLabFile();
        }

        public override string CategoryCaption => "Unity";

        public override string Present(ITemplateScopePoint point)
        {
            if (point is InUnityShaderLabFile)
                return "Anywhere in Unity ShaderLab file";
            return base.Present(point);
        }
    }
}