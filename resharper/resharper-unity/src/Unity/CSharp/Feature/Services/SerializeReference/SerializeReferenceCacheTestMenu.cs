#nullable enable
using System.IO;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.DataContext;
using JetBrains.Application.Diagnostics;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.Actions.InternalMenu;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Psi.Files;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.SerializeReference
{
    [ZoneMarker(typeof(IInternalVisibilityZone))]
    [ActionGroup(ActionGroupInsertStyles.Submenu | ActionGroupInsertStyles.Separated, Text = "Unity Serialization")]
    public class SerializeReferenceCacheTestMenu : IAction, IInsertAfter<IntoInternalMenu, InternalProjectsMenu>
    {
        public SerializeReferenceCacheTestMenu(SerializeReferenceDumpAction serializeReferenceDumpAction)
        {
        }
    }

    [ZoneMarker(typeof(IInternalVisibilityZone))]
    [Action(nameof(SerializeReferenceDumpAction), nameof(SerializeReferenceDumpAction))]
    public class SerializeReferenceDumpAction : IExecutableAction
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            return context.TryGetComponent<ISolution>() != null;
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            var solution = context.GetComponent<ISolution>();
            var psiFiles = solution.GetComponent<IPsiFiles>();
            var serializedReferenceProvider = solution.GetComponent<IUnitySerializedReferenceProvider>();

            var contextDumper = context.TryGetComponent<IContextDumper>();
            if (contextDumper != null)
                contextDumper.Dump(context, Dump);
            else
                Dumper.DumpToNotepad(Dump);


            void Dump(StreamWriter writer)
            {
                Assertion.AssertNotNull(writer, "StreamWriter is null");
                psiFiles.CommitAllDocumentsAsync(() =>
                {
                    var swa = solution.GetComponent<SolutionAnalysisService>();
                    if (swa.Configuration?.Completed?.Value != true)
                    {
                        writer.WriteLine("SWA is not completed");
                        return;
                    }

                    serializedReferenceProvider.DumpFull(writer, solution);
                }, () => writer.WriteLine("Failed to commit all documents"));
            }
        }
    }
}