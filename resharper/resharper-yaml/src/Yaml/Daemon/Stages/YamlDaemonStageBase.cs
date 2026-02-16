using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Stages
{
  public abstract class YamlDaemonStageBase : IModernDaemonStage
  {
    private const int LargeFileThreshold = 1 * 1024 * 1024;

    public IEnumerable<IDaemonStageProcess> CreateProcess(
      IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind)
    {
      if (!IsSupported(process.SourceFile))
        return EmptyList<IDaemonStageProcess>.InstanceList;

      process.SourceFile.GetPsiServices().Files.AssertAllDocumentAreCommitted();

      return process.SourceFile.GetPsiFiles<YamlLanguage>()
        .SelectNotNull(file => CreateProcess(process, settings, processKind, (IYamlFile) file));
    }

    protected abstract IDaemonStageProcess CreateProcess(
      IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind, IYamlFile file);

    bool IModernDaemonStage.IsApplicable(IPsiSourceFile sourceFile, DaemonProcessKind processKind)
    {
      return IsSupported(sourceFile);
    }

    protected virtual bool IsSupported(IPsiSourceFile sourceFile)
    {
      var properties = sourceFile.Properties;
      if (properties.IsNonUserFile || !properties.ProvidesCodeModel)
        return false;

      return sourceFile.IsLanguageSupported<YamlLanguage>();
    }

    protected virtual bool ShouldAllowOpeningChameleons(IYamlFile file, DaemonProcessKind processKind)
    {
      // By default, only process already open chameleons for files larger than 1Mb when doing background analysis
      // (SWEA). Opening chameleons for e.g. large Unity files is too expensive
      if (processKind != DaemonProcessKind.VISIBLE_DOCUMENT
          && file.GetSourceFile().ToProjectFile() is ProjectFileImpl projectFileImpl
          && projectFileImpl.CachedFileSystemData.FileLength > LargeFileThreshold)
      {
        return false;
      }

      return true;
    }
  }
}