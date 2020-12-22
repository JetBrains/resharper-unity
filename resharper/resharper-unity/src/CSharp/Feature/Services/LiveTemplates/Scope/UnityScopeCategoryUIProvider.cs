using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    [ScopeCategoryUIProvider(Priority = Priority)]
    public class UnityScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
        // Needs to be less than other priorities in R#'s built in ScopeCategoryUIProvider
        // to push it to the end of the list
        private const int Priority = -200;

        public UnityScopeCategoryUIProvider()
            : base(LogoIcons.Unity.Id)
        {
            MainPoint = new InUnityShaderLabFile();
        }

        public override IEnumerable<ITemplateScopePoint> BuildAllPoints()
        {
            yield return new InUnityCSharpProject();
            yield return new MustBeInUnitySerializableType();
            yield return new MustBeInUnityType();
            yield return new MustBeInUnityCSharpFile();
            
            // TODO: Should this be part of this category provider? Everything else is C#
            yield return new InUnityShaderLabFile();
        }

        public override string CategoryCaption => "Unity";

        public override string Present(ITemplateScopePoint point)
        {
            switch (point)
            {
                case InUnityCSharpProject _:
                    return "In Unity project";
                case MustBeInUnityCSharpFile _:
                    return "In Unity C# file";
                case MustBeInUnityType _:
                    return "In Unity type where type members are allowed";
                case MustBeInUnitySerializableType _:
                    return "In Unity serializable type where type members are allowed";
                case InUnityShaderLabFile _:
                    return "In Unity ShaderLab file";
                default:
                    return base.Present(point);
            }
        }
    }
}