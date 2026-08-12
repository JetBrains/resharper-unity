using JetBrains.NUnitExtensions;

namespace JetBrains.ReSharper.Plugins.Tests.Unity;

public class UnityPluginChecklistAttribute(params string[] checklistItems) : 
    ChecklistAttribute("rider/test/cases/testData/checklists/Unity/Plugin.md",checklistItems){}
