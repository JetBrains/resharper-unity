using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ChatContexts.Usages;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.AI;

[SolutionComponent(InstantiationEx.LegacyDefault)]
public class AssetOccurrenceToSymbolUsageAdapter : IOccurrenceToSymbolUsageAdapter
{
    public SymbolUsageOccurrence Transform(string clrName, IDeclaredElement declaredElement, IOccurrence occurrence)
    {
        if (occurrence is not UnityAssetOccurrence assetOccurrence)
            return null;

        return new AssetSymbolUsageOccurrence(assetOccurrence.SourceFile.GetLocation().FullPath,
            clrName, declaredElement.ShortName, assetOccurrence.GetDisplayText().Text);
    }
}

public class AssetSymbolUsageOccurrence : SymbolUsageOccurrence, ISymbolUsageOccurrenceWithCustomPresentation
{
    private readonly string myAbsoluteFilePath;
    private readonly string myDisplayName;
    private readonly string myLine;

    public AssetSymbolUsageOccurrence(string absoluteFilePath, string clrName, string displayName, string line) :
        base(absoluteFilePath, clrName, displayName, line)
    {
        myAbsoluteFilePath = absoluteFilePath;
        myDisplayName = displayName;
        myLine = line;
    }

    public string AbsoluteFilePath => myAbsoluteFilePath;
    public string DisplayName => myDisplayName;
    public string Line => myLine;

    public string PresentToModel()
    {
        return $"Scene file: {AbsoluteFilePath}\n" +
               $"Scene path to usage: {Line}\n";
    }
}