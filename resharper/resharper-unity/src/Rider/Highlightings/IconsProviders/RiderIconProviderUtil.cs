using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Collections.Viewable;
using JetBrains.ReSharper.Host.Platform.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders
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

        public static List<CodeLensEntryExtraActionModel> GetExtraActions(UnitySolutionTracker solutionTracker,
                                                                          BackendUnityHost backendUnityHost)
        {
            var extraActions = new List<CodeLensEntryExtraActionModel>();
            if (solutionTracker.IsUnityProject.HasTrueValue() && !backendUnityHost.IsConnectionEstablished())
            {
                extraActions.Add(new CodeLensEntryExtraActionModel("Unity is not running", null));
                extraActions.Add(new CodeLensEntryExtraActionModel("Start Unity Editor",
                    AbstractUnityCodeInsightProvider.StartUnityActionId));
            }

            return extraActions;
        }
    }
}