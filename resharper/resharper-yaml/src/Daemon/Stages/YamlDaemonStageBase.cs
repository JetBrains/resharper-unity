using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
{
  public abstract class YamlDaemonStageBase : IDaemonStage
  {
    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
      DaemonProcessKind processKind)
    {
      if (!IsSupported(process.SourceFile))
        return EmptyList<IDaemonStageProcess>.InstanceList;

      process.SourceFile.GetPsiServices().Files.AssertAllDocumentAreCommitted();

      return process.SourceFile.GetPsiFiles<YamlLanguage>()
        .SelectNotNull(file => CreateProcess(process, settings, processKind, (IYamlFile) file));
    }

    protected abstract IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
      DaemonProcessKind processKind, IYamlFile file);

    protected virtual bool IsSupported(IPsiSourceFile sourceFile)
    {
      if (sourceFile == null || !sourceFile.IsValid())
        return false;

      var properties = sourceFile.Properties;
      if (properties.IsNonUserFile || !properties.ProvidesCodeModel)
        return false;

      return sourceFile.IsLanguageSupported<YamlLanguage>();
    }
  }
}