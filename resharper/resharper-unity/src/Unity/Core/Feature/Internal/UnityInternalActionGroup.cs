using JetBrains.Application.UI.Actions.InternalMenu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Internal
{
    [ActionGroup(ActionGroupInsertStyles.Submenu | ActionGroupInsertStyles.Separated, Text = "Unity")]
    public class UnityInternalActionGroup : IAction, IInsertLast<IntoInternalMenu>
    {
        public UnityInternalActionGroup(DumpDuplicateTypeNamesAction dumpDuplicateTypeNamesAction)
        {
        }
    }
}