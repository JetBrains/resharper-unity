using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [Action(typeof(Strings), nameof(Strings.FindUnityUsagesText), Id = 9000)]
    public class GoToUnityUsagesAction : ContextNavigationActionBase<GoToUnityUsagesProvider>
    {
    }
}
