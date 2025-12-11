#nullable enable
using System;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

public static class ProfilerSnapshotFetchingSettingsExtensions
{
    public static ProfilerGutterMarkRenderSettings ToProfilerGutterMarkRenderSettings(
        this ProfilerSnapshotHighlightingSettings settings)
    {
        return settings switch
        {
            ProfilerSnapshotHighlightingSettings.Hidden => ProfilerGutterMarkRenderSettings.Hidden,
            ProfilerSnapshotHighlightingSettings.Default => ProfilerGutterMarkRenderSettings.Default,
            ProfilerSnapshotHighlightingSettings.Minimized => ProfilerGutterMarkRenderSettings.Minimized,
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings, null)
        };
    }
    
    public static ProfilerSnapshotHighlightingSettings ToProfilerSnapshotHighlightingSettings(
        this ProfilerGutterMarkRenderSettings settings)
    {
        return settings switch
        {
            ProfilerGutterMarkRenderSettings.Hidden => ProfilerSnapshotHighlightingSettings.Hidden,
            ProfilerGutterMarkRenderSettings.Default => ProfilerSnapshotHighlightingSettings.Default,
            ProfilerGutterMarkRenderSettings.Minimized => ProfilerSnapshotHighlightingSettings.Minimized,
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings, null)
        };
    }
}