using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes
{
    // Required for IncorrectMethodSignatureQuickFix, which needs to use InplaceRefactoringsHighlightingManager
    // Looks like we can't apply a zone to a [QuickFix] component, only to [Component] classes
    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>
    {
    }
}