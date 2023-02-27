﻿using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Daemon.SolutionAnalysis;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Json;
using JetBrains.ReSharper.Plugins.Yaml;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ZoneDefinition(ZoneFlags.AutoEnable)]
    public interface IUnityPluginZone : IZone,
        IRequire<DaemonZone>,
        IRequire<NavigationZone>,
        IRequire<ICodeEditingZone>,
        IRequire<ExternalSourcesZone>,
        IRequire<ILanguageJsonNewZone>,
        IRequire<ILanguageCSharpZone>,
        IRequire<PsiFeaturesImplZone>,
        IRequire<ILanguageYamlZone>,
        IRequire<SweaZone>
    {
    }

    [ZoneMarker]
    public class ZoneMarker : IRequire<IUnityPluginZone>
    {
    }
}