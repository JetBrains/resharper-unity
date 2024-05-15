#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.ShaderKeywordsHighlighting_Text), IsVisible: true)]
public static class ShaderKeywordsHighlightingId
{
}