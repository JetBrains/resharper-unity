using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;

public interface IAssetOccurrenceWithTextOccurrence : IOccurrence
{
    public IPsiSourceFile GetSourceFile();
    public TextRange RenameTextRange { get; }
}