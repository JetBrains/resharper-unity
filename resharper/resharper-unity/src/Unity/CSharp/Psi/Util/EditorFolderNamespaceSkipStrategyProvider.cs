#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Util;

/// <summary>
/// Unity has the convention of placing editor-only code into the Editor subfolders, which makes it unavailable for run-time,
/// however, it's often not desirable to have an actual Editor namespace, because its name can clash with the commonly used
/// UnityEditor.Editor class, so we provide a setting to omit any Editor folders from namespace calculation.
/// </summary>
[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class EditorFolderNamespaceSkipStrategyProvider : INamespaceFolderSkipStrategyProvider
{
    private Strategy? myStrategy;

    public INamespaceFolderSkipStrategy? GetNamespaceFolderSkipStrategy(IProjectItem projectItem, PsiLanguageType language)
    {
        if (!language.Is<CSharpLanguage>()) return null;

        var project = projectItem.GetProject();
        if (project == null || !project.IsUnityProject()) return null;

        var settingsStore = project.GetSolution().GetSettingsStore();
        var featureEnabled = settingsStore.GetValue((UnitySettings s) => s.SkipEditorFoldersForNamespaceCalculation);
        if (!featureEnabled) return null;

        myStrategy ??= new Strategy();
        return myStrategy;
    }

    private class Strategy : INamespaceFolderSkipStrategy
    {
        public bool ShouldSkipFolder(IProjectFolder folder) => folder.Name == "Editor";
    }
}
