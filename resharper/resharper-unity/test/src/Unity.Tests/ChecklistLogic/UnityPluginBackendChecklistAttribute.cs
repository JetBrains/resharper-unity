using JetBrains.NUnitExtensions;

namespace JetBrains.ReSharper.Plugins.Tests.Unity;

public class UnityPluginBackendChecklistAttribute(params string[] checklistItems) :
    ChecklistAttribute(ChecklistPath,checklistItems)
{
    public const string ChecklistPath = "rider/test/cases/testData/checklists/Unity/UnityPlugin(backend).md";
}
