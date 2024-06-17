#nullable enable
using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.LiveTemplates.Scope
{
    [ScopeCategoryUIProvider(Priority = Priority)]
    public class UnityScopeCategoryUIProvider : ScopeCategoryUIProvider
    {
        private readonly IReadOnlyList<IUnityAdditionalTemplateScopePointsProvider> myScopePointsProviders; 
        
        // Needs to be less than other priorities in R#'s built in ScopeCategoryUIProvider
        // to push it to the end of the list
        private const int Priority = -200;

        public UnityScopeCategoryUIProvider(ILazyImmutableList<IUnityAdditionalTemplateScopePointsProvider> scopePointsProviders)
            : base(LogoIcons.Unity.Id)
        {
            MainPoint = new InUnityCSharpProject();
            myScopePointsProviders = scopePointsProviders;
        }

        public override IEnumerable<ITemplateScopePoint> BuildAllPoints()
        {
            yield return new InUnityCSharpProject();
            yield return new MustBeInUnitySerializableType();
            yield return new MustBeInUnityType();
            yield return new MustBeInUnityCSharpFile();
            foreach (var provider in myScopePointsProviders)
            {
                foreach (var scopePoint in provider.GetUnityScopePoints())
                    yield return scopePoint;
            }
        }

        public override string CategoryCaption => "Unity";

        public override bool HaveOptionsUIFor(ITemplateScopePoint point)
        {
            foreach (var scopePointsProvider in myScopePointsProviders)
            {
                if (scopePointsProvider.HaveOptionsUIFor(point))
                    return true;
            }

            return false;
        }

        public override IScopeOptionsUIBase? CreateUI(ITemplateScopePoint point)
        {
            foreach (var scopePointsProvider in myScopePointsProviders)
            {
                if (scopePointsProvider.CreateUI(point) is {} ui)
                    return ui;
            }

            return null;
        }

        public override string Present(ITemplateScopePoint point)
        {
            foreach (var provider in myScopePointsProviders)
            {
                if (provider.TryPresent(point, out var presentation))
                    return presentation;
            }
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
                default:
                    return base.Present(point);
            }
        }
    }
}