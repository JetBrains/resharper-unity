#nullable enable
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

public interface IInactiveShaderBranchHighlightFactory
{
    IHighlighting CreateInactiveShaderBranchHighlight(DocumentRange documentRange, ICollection<string> scopeKeywords);
}