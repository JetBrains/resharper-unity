using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.RdBackend.Common.Platform.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.Rider.Model;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public static class RiderIconProviderUtil
    {
        public static bool IsCodeVisionEnabled(IContextBoundSettingsStore settings, string providerId, Action fallback, out bool useFallback)
        {
            useFallback = false;
            if (settings.GetIndexedValue((CodeInsightsSettings key) => key.DisabledProviders, providerId))
            {
                fallback();
                useFallback = true;
                return false;
            }

            if (settings.GetValue((UnitySettings key) => key.GutterIconMode) == GutterIconMode.Always)
            {
                fallback();
                useFallback = true;
            }
            return true;
        }

        public static List<CodeVisionEntryExtraActionModel> GetExtraActions(UnitySolutionTracker solutionTracker,
                                                                          IBackendUnityHost backendUnityHost)
        {
            var extraActions = new List<CodeVisionEntryExtraActionModel>();
            if (solutionTracker.IsUnityProject.HasTrueValue() && !backendUnityHost.IsConnectionEstablished())
            {
                extraActions.Add(new CodeVisionEntryExtraActionModel(Strings.UnityIsNotRunning_Text, null));
                extraActions.Add(new CodeVisionEntryExtraActionModel(Strings.RiderIconProviderUtil_GetExtraActions_Start_Unity_Editor,
                    AbstractUnityCodeInsightProvider.StartUnityActionId));
            }

            return extraActions;
        }
    }
}