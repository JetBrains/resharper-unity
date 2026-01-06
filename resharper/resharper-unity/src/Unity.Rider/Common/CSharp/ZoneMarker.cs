using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.DocumentModel;
using JetBrains.RdBackend.Common.Env;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ILanguageCSharpZone>,
        IRequire<IReSharperHostCoreSharedFeatureZone>,
        IRequire<PsiFeaturesImplZone>,
        IRequire<ITextControlsProtocolZone>,
        IRequire<IDocumentModelZone>
    {
    }
}