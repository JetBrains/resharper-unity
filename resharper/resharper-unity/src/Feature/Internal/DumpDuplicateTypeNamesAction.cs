using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Diagnostics;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Internal
{
    // Simple internal action to dump any clashing APIs in the given project's references. Useful for finding extra
    // candidates for values to add to AutoImportSolutionSettingsProvider
    [Action("Unity_Internal_DumpDuplicateTypeNames", "Dump Duplicate Type Names")]
    public class DumpDuplicateTypeNamesAction : IExecutableAction
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            // TODO: Make sure it's only visible/available in internal mode?
            return true;
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            var solution = context.GetData(ProjectModelDataConstants.SOLUTION);
            if (solution == null)
                return;

            var symbolCache = solution.GetComponent<ISymbolCache>();

            var types = new OneToSetMap<string, string>();
            var symbolScope = symbolCache.GetSymbolScope(LibrarySymbolScope.FULL, true);
            foreach (var typeElement in symbolScope.GetAllTypeElementsGroupedByName().OfType<ITypeMember>())
            {
                // Non-nested, public or protected types. Essentially what would appear in completion
                if (typeElement.GetContainingType() == null && IsVisible(typeElement))
                {
                    var typeParametersOwner = (ITypeParametersOwner) typeElement;
                    var shortName = typeElement.ShortName + (typeParametersOwner.TypeParameters.Count > 0
                                        ? $"`{typeParametersOwner.TypeParameters.Count}"
                                        : string.Empty);
                    types.Add(shortName, ((ITypeElement) typeElement).GetClrName().FullName);
                }
            }

            Dumper.DumpToNotepad(writer =>
            {
                var toDump = (from pair in types
                    where pair.Value.Count > 1 && pair.Value.Any(x => !x.StartsWith("System"))
                    let skipped = IsSkipped(pair)
                    select new {pair, skipped}).OrderBy(x => x.pair.Key).ToList();

                writer.WriteLine($"Found {toDump.Count} items. Skipping {toDump.Count(x => x.skipped)}");
                writer.WriteLine();

                foreach (var (key, value) in toDump.Where(x => !x.skipped).Select(x => x.pair))
                {
                    writer.WriteLine("Short name: " + key);
                    foreach (var fullName in value) writer.WriteLine($"  {fullName}");
                    writer.WriteLine();
                }

                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine("Skipping:");
                writer.WriteLine();

                foreach (var (key, value) in toDump.Where(x => x.skipped).Select(x => x.pair))
                {
                    writer.WriteLine("Short name: " + key);
                    foreach (var fullName in value) writer.WriteLine($"  {fullName}");
                    writer.WriteLine();
                }
            });
        }

        private bool IsSkipped(KeyValuePair<string, ISet<string>> pair)
        {
            return pair.Value.Count(x => !IsSkipped(x)) < 2;
        }

        private bool IsSkipped(string fullName)
        {
            // Note that this replicates the settings in AutoImportSolutionSettingsProvider, and is just a means of
            // making it easier to check the list of matching items
            return fullName.StartsWith("Boo.Lang.") || fullName.StartsWith("UnityScript.") ||
                   fullName.StartsWith("System.Diagnostics.Debug");
        }

        private static bool IsVisible(ITypeMember typeElement)
        {
            return typeElement.AccessibilityDomain.DomainType == AccessibilityDomain.AccessibilityDomainType.PUBLIC
                   || typeElement.AccessibilityDomain.DomainType ==
                   AccessibilityDomain.AccessibilityDomainType.PROTECTED;
        }
    }
}