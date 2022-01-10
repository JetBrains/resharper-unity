using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [Action("FindUnityUsages", "Find Unity Usages", Id = 9000)]
    public class GoToUnityUsagesAction : ContextNavigationActionBase<GoToUnityUsagesProvider>
    {
    
    }
}