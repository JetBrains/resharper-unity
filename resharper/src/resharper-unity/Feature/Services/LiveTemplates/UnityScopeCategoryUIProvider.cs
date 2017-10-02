using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.LiveTemplates
{
    [ScopeCategoryUIProvider(Priority = -200)]
    public class UnityScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
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
    }
}