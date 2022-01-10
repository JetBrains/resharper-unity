namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    public interface IUnityHighlighting
    {
    }

    // Use this to mark highlightings that are added from analysis, e.g. warnings and errors. This is useful for
    // filtering highlightings for analyser tests, especially because it allows ignoring indicator highlightings, such
    // as Code Vision, gutter icons, and performance indicators, which have different behaviour in Rider and ReSharper
    public interface IUnityAnalyzerHighlighting : IUnityHighlighting
    {
    }

    // Marks a highlight as an indicator, rather than the result of analysis. E.g. gutter icons, code vision, and
    // performance indicators. These indicators have different behaviour in Rider and ReSharper, because ReSharper
    // doesn't support Code Vision or EffectType.LINE_MARKER
    public interface IUnityIndicatorHighlighting : IUnityHighlighting
    {
    }
}