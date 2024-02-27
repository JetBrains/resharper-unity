#nullable enable
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;

public interface IShaderVariantsHost
{
    void ShowShaderVariantInteraction(DocumentOffset offset, ShaderVariantInteractionOrigin origin, IEnumerable<string>? scopeKeywords);
}