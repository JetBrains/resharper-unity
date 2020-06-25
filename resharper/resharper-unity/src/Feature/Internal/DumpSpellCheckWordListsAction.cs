using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Diagnostics;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Features.ReSpeller.SpellEngine;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Internal
{
    [Action("Unity_Internal_DumpSpellCheckWordLists", "Dump Spell Check Word Lists")]
    public class DumpSpellCheckWordListsAction : IExecutableAction
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            return true;
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            var solution = context.GetData(ProjectModelDataConstants.SOLUTION);
            if (solution == null)
                return;

            var wordsPerAssembly = new OneToSetMap<string, string>(valueComparer: StringComparer.InvariantCultureIgnoreCase);
            var abbreviations = new JetHashSet<string>();

            // TODO: This should be in a background task
            var psiModules = solution.GetComponent<IPsiModules>();
            var symbolCache = solution.GetComponent<ISymbolCache>();
            foreach (var assemblyPsiModule in psiModules.GetAssemblyModules().OrderBy(m => m.DisplayName))
            {
                if (!ShouldProcessAssembly(assemblyPsiModule))
                    continue;

                var symbolScope = symbolCache.GetSymbolScope(assemblyPsiModule, false, true);
                var typeElements = symbolScope.GetAllTypeElementsGroupedByName();
                foreach (var typeElement in typeElements)
                {
                    if (!typeElement.CanBeVisibleToSolution())
                        continue;

                    AddWords(wordsPerAssembly, abbreviations, assemblyPsiModule, typeElement.ShortName);

                    foreach (var namespaceName in typeElement.GetClrName().NamespaceNames)
                        AddWords(wordsPerAssembly, abbreviations, assemblyPsiModule, namespaceName);

                    // TODO: Should we skip enum values?
                    // It's unlikely that a user would name a method or variable after a value such as AdvertisingNetwork.Aarki,
                    // but on the other hand, if it's used in a comment, it'll show up as a typo.

                    foreach (var typeMember in typeElement.GetMembers())
                    {
                        if (!typeMember.CanBeVisibleToSolution() || typeMember is IConstructor)
                            continue;

                        // Don't use enum values as abbreviations - avoids false positives with ALL_UPPER_CASE style
                        var abbr = typeElement is IEnum ? new JetHashSet<string>() : abbreviations;
                        AddWords(wordsPerAssembly, abbr, assemblyPsiModule, typeMember.ShortName);

                        // TODO: Include parameter names?
                        // Respeller will not check parameter names of overriding or implementing functions, so this is
                        // probably unnecessary
                    }
                }
            }

            // Case insensitive. There will not be duplicates
            var allWords = (from pair in wordsPerAssembly
                from word in pair.Value
                select word).ToJetHashSet(StringComparer.InvariantCultureIgnoreCase);

            var spellService = solution.GetComponent<ISpellService>();

            var unknownWords = (from word in allWords
                where !spellService.CheckWordSpelling(word)
                select word).ToJetHashSet(StringComparer.InvariantCultureIgnoreCase);

            // Add abbreviations separately. If added to the dictionary, we don't get typo warnings, but we can get
            // naming standard inspection warnings. E.g. BlahBlahAABB is converted to BlahBlahAabb. Don't add anything
            // that's already known, but the spell checker will only check for words of 4 characters or more.
            // TODO: Ideally, we should disable AbbreviationsSettingsProvider. But just merge results in for now
            var unknownAbbreviations = (from word in abbreviations
                where (word.Length > 1 && word.Length < 4) || !spellService.CheckWordSpelling(word)
                select word).ToJetHashSet();

            // Remove the non-abbreviations
            unknownAbbreviations.Remove("TEXTMESHPRO");

            // Dump all words for diagnostics
            Dumper.DumpToNotepad(w =>
            {
                w.WriteLine("All words");
                w.WriteLine();

                foreach (var (assembly, words) in wordsPerAssembly)
                {
                    w.WriteLine("Words for assembly: " + assembly);
                    w.WriteLine();

                    foreach (var word in words.OrderBy(IdentityFunc<string>.Instance))
                        w.WriteLine(word);
                    w.WriteLine();
                }
            });

            // Dump all abbreviations, so we can avoid naming standards inspections when an abbreviation is used.
            // E.g. BlahBlahAABB is accepted instead of converted to BlahBlahAabb
            Dumper.DumpToNotepad(w =>
            {
                w.WriteLine("Abbreviations");
                w.WriteLine();

                foreach (var abbreviation in unknownAbbreviations.OrderBy(IdentityFunc<string>.Instance))
                    w.WriteLine(abbreviation);
            });

            // Dump all unknown words, minus abbreviations, for use as a dictionary
            Dumper.DumpToNotepad(w =>
            {
                var dictionary = new JetHashSet<string>(unknownWords, StringComparer.InvariantCultureIgnoreCase);
                dictionary.ExceptWith(abbreviations);

                w.WriteLine("Dictionary (unknown words minus abbreviations)");
                w.WriteLine();
                w.WriteLine(dictionary.Count);    // Hunspell dictionaries start with the number of words

                foreach (var word in dictionary.OrderBy(IdentityFunc<string>.Instance))
                    w.WriteLine(word);
            });
        }

        private static bool ShouldProcessAssembly(IAssemblyPsiModule assemblyPsiModule)
        {
            var name = assemblyPsiModule.Assembly.AssemblyName.Name;
            return !name.StartsWith("System") && !name.StartsWith("Microsoft") && name != "netstandard" &&
                   name != "JetBrains.Annotations";
        }

        private static void AddWords(OneToSetMap<string, string> wordsPerAssembly, JetHashSet<string> abbreviations,
            IAssemblyPsiModule assemblyPsiModule, string name)
        {
            var textParts = TextSplitter.Split(name);
            foreach (var textPart in textParts.Where(tp => tp.Type == TextPartType.Word))
            {
                if (textPart.Text == textPart.Text.ToUpperInvariant())
                    abbreviations.Add(textPart.Text);
                else
                    wordsPerAssembly.Add(assemblyPsiModule.Assembly.AssemblyName.Name, textPart.Text);
            }
        }
    }
}