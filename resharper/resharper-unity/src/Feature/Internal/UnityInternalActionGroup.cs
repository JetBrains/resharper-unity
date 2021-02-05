using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.IDE.Internal;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Internal
{
    [ActionGroup(ActionGroupInsertStyles.Submenu | ActionGroupInsertStyles.Separated, Text = "Unity")]
    public class UnityInternalActionGroup : IAction, IInsertLast<IntoInternalMenu>
    {
        public UnityInternalActionGroup(DumpDuplicateTypeNamesAction dumpDuplicateTypeNamesAction,
            DumpSpellCheckWordListsAction dumpSpellCheckWordListsAction)
        {
        }
    }
}