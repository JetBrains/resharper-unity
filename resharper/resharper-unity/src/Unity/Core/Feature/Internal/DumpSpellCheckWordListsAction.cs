using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.DataContext;
using JetBrains.Application.Diagnostics;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Features.ReSpeller;
using JetBrains.ReSharper.Features.ReSpeller.SpellEngine;
using JetBrains.ReSharper.Features.ReSpeller.SpellEngine.SpellBackend;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.Extension;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Internal
{
    [ZoneMarker(typeof(IReSpellerZone))]
    [Action(typeof(Strings), nameof(Strings.Unity_Internal_DumpSpellCheckWordLists_Text))]
    public class DumpSpellCheckWordListsAction : IExecutableAction, IInsertLast<UnityInternalActionGroup>
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
            var abbreviationsWithOriginalWord = new OneToSetMap<string, string>(valueComparer: StringComparer.InvariantCultureIgnoreCase);

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

                    AddWords(wordsPerAssembly, abbreviationsWithOriginalWord, assemblyPsiModule,
                        typeElement is IInterface ? typeElement.ShortName.RemoveStart("I") : typeElement.ShortName);

                    foreach (var namespaceName in typeElement.GetClrName().NamespaceNames)
                        AddWords(wordsPerAssembly, abbreviationsWithOriginalWord, assemblyPsiModule, namespaceName);

                    // TODO: Should we skip enum values?
                    // It's unlikely that a user would name a method or variable after a value such as AdvertisingNetwork.Aarki,
                    // but on the other hand, if it's used in a comment, it'll show up as a typo.

                    foreach (var typeMember in typeElement.GetMembers())
                    {
                        if (!ShouldProcessTypeMember(typeMember))
                            continue;

                        // Don't use any enum values as abbreviations. Avoids false positives with ALL_UPPER_CASE style
                        AddWords(wordsPerAssembly,
                            typeElement is IEnum ? new OneToSetMap<string, string>() : abbreviationsWithOriginalWord,
                            assemblyPsiModule, typeMember.ShortName);

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

            var spellService = solution.GetComponent<ISpellServiceBackendDispatcher>().SpellService;

            var wordsCheckResult = spellService.CheckWords(
                Lifetime.Eternal,
                allWords.Concat(abbreviationsWithOriginalWord.Keys).AsList()
            );
            if (wordsCheckResult == null)
            {
                var logger = Logger.GetLogger<DumpSpellCheckWordListsAction>();
                logger.Error("Failed to check words");
                return;
            }
            var unknownWords = (from word in allWords
                where !wordsCheckResult.TryGetValue(word)
                select word).ToJetHashSet(StringComparer.InvariantCultureIgnoreCase);

            // Add abbreviations separately. If added to the dictionary, we don't get typo warnings, but we can get
            // naming standard inspection warnings. E.g. BlahBlahAABB is converted to BlahBlahAabb. Don't add anything
            // that's already known, but the spell checker will only check for words of 4 characters or more.
            // TODO: Ideally, we should disable AbbreviationsSettingsProvider, or we'll ignore our own abbreviations
            // Merge files by hand
            var unknownAbbreviations = (from word in abbreviationsWithOriginalWord.Keys
                where (word.Length > 1 && word.Length < 4) || !wordsCheckResult.TryGetValue(word)
                select word).ToJetHashSet();

            Dumper.DumpToNotepad(w =>
            {
                w.WriteLine("Abbreviations:");
                w.WriteLine();

                foreach (var (abbr, originalWords) in abbreviationsWithOriginalWord.OrderBy(kv => kv.Key))
                {
                    w.Write(abbr);
                    w.Write(": ");
                    foreach (var word in originalWords)
                    {
                        w.Write(word);
                        w.Write(", ");
                    }
                    w.WriteLine();
                }
            });

            // Remove non-abbreviations, known typos or exclusions. Yes, this is all done by hand
            RemoveNonAbbreviations(unknownAbbreviations);
            RemoveTyposAndExclusions(unknownWords);

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
                        w.WriteLine(word.ToLowerInvariant());
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
                    w.WriteLine(abbreviation.ToUpperInvariant());
            });

            // Dump all unknown words, minus abbreviations, for use as a dictionary
            Dumper.DumpToNotepad(w =>
            {
                var dictionary = new JetHashSet<string>(unknownWords, StringComparer.InvariantCultureIgnoreCase);
                dictionary.ExceptWith(abbreviationsWithOriginalWord.Keys);

                w.WriteLine("Dictionary (unknown words minus abbreviations)");
                w.WriteLine();
                w.WriteLine(dictionary.Count);    // Hunspell dictionaries start with the number of words

                foreach (var word in dictionary.OrderBy(IdentityFunc<string>.Instance))
                    w.WriteLine(word.ToLowerInvariant());
            });
        }

        private static bool ShouldProcessAssembly(IAssemblyPsiModule assemblyPsiModule)
        {
            var name = assemblyPsiModule.Assembly.AssemblyName.Name;
            return !name.StartsWith("System") && !name.StartsWith("Microsoft") && name != "netstandard" &&
                   name != "mscorlib" && !name.StartsWith("nunit") && !name.StartsWith("Boo") &&
                   name != "unityplastic" && !name.StartsWith("UnityScript", StringComparison.InvariantCultureIgnoreCase) &&
                   name != "JetBrains.Annotations" && name != "Unity.Rider.Editor";
        }

        private static bool ShouldProcessTypeMember(ITypeMember typeMember)
        {
            // Note that UnityEditor has a lot of InternalsVisibleTo, including for Unity.Collections and Unity.Entities
            // so make sure these projects are not part of the solution used to run this action!
            if (!typeMember.CanBeVisibleToSolution() || typeMember is IConstructor || typeMember is IAccessor)
                return false;

            // Ignore Unity.Mathematics swizzling operators (all combinations of x,y,z and w)
            if (IsSwizzlingProperty(typeMember))
                return false;

            if (typeMember.GetContainingType() is IEnum enumTypeElement)
            {
                if (typeMember.ShortName == "iPhoneAndiPad" || typeMember.ShortName == "SetiPhoneLaunchScreenType")
                    return false; // "andi", "seti"

                // Don't do anything with the AdvertisingNetwork enum. It's full of weird names that would be nice to
                // not show typos for in comments (e.g. AerServ), but we have to split that into words, so we get "aer"
                // and "serv" as words in the dictionary, which are not useful
                // Same goes for Stores
                if (enumTypeElement.GetClrName().FullName == "UnityEngine.Analytics.AdvertisingNetwork"
                    || enumTypeElement.GetClrName().FullName == "UnityEngine.Monetization.Store")
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSwizzlingProperty(ITypeMember typeMember)
        {
            // E.g. Unity.Mathematics.bool2.wwww
            if (!(typeMember is IProperty))
                return false;

            if (typeMember.GetContainingType()?.GetContainingNamespace().QualifiedName != "Unity.Mathematics")
                return false;

            if (typeMember.ShortName.Length != 4)
                return false;

            return typeMember.ShortName.All(letter => letter == 'x' || letter == 'y' || letter == 'z' || letter == 'w');
        }

        private static void AddWords(OneToSetMap<string, string> wordsPerAssembly,
                                     OneToSetMap<string, string> abbreviationsWithOriginalWord,
                                     IAssemblyPsiModule assemblyPsiModule, string name)
        {
            var textParts = TextSplitter.Split(name);
            foreach (var textPart in textParts.Where(tp => tp.Type == TextPartType.Word))
            {
                if (textPart.Text == textPart.Text.ToUpperInvariant())
                {
                    if (textPart.Text.Length > 1)
                        abbreviationsWithOriginalWord.Add(textPart.Text, name);
                }
                else
                    wordsPerAssembly.Add(assemblyPsiModule.Assembly.AssemblyName.Name, textPart.Text);
            }
        }

        private static void RemoveNonAbbreviations(ICollection<string> unknownAbbreviations)
        {
            // Remove known words (which would be done by the spell checker, but that only works for a min of 4 letters)
            // ReSharper disable StringLiteralTypo
            unknownAbbreviations.Remove("ADD");
            unknownAbbreviations.Remove("AND");
            unknownAbbreviations.Remove("GET");
            unknownAbbreviations.Remove("GO");
            unknownAbbreviations.Remove("GRAIDENT");    // Typo fixed in later versions
            unknownAbbreviations.Remove("KEY");
            unknownAbbreviations.Remove("LO");  // GetLODs
            unknownAbbreviations.Remove("MAX");
            unknownAbbreviations.Remove("MIN");
            unknownAbbreviations.Remove("OFF");
            unknownAbbreviations.Remove("ON");
            unknownAbbreviations.Remove("PUT");
            unknownAbbreviations.Remove("TEXTMESHPRO");
            unknownAbbreviations.Remove("UP");
            // ReSharper restore StringLiteralTypo
        }

        private static void RemoveTyposAndExclusions(ICollection<string> unknownWords)
        {
            // ReSharper disable StringLiteralTypo
            unknownWords.Remove("adaptor");
            unknownWords.Remove("contoller");
            unknownWords.Remove("corlib");
            unknownWords.Remove("deprected");
            unknownWords.Remove("doesnt");
            unknownWords.Remove("dont");
            unknownWords.Remove("hugarian");
            unknownWords.Remove("iconwarning");
            unknownWords.Remove("joyn");
            unknownWords.Remove("jvalue");
            unknownWords.Remove("memoryless");
            unknownWords.Remove("mikk");
            unknownWords.Remove("modelview");
            unknownWords.Remove("normalizesafe");
            unknownWords.Remove("occlucion");
            unknownWords.Remove("pptr");
            unknownWords.Remove("projectsafe");
            unknownWords.Remove("ptitle");
            unknownWords.Remove("quat");
            unknownWords.Remove("recieve");
            unknownWords.Remove("recv");
            unknownWords.Remove("regen");
            unknownWords.Remove("resonace");
            unknownWords.Remove("rolloff");
            unknownWords.Remove("serbo");
            unknownWords.Remove("smallcaps");
            unknownWords.Remove("smartupdate");
            unknownWords.Remove("smoothstep");
            unknownWords.Remove("textfirst");
            unknownWords.Remove("texure");
            unknownWords.Remove("theshold");
            unknownWords.Remove("timeu");
            unknownWords.Remove("treshold");
            unknownWords.Remove("vdecl");
            unknownWords.Remove("xywh");
            unknownWords.Remove("zoomer");

            // Maths
            unknownWords.Remove("asdouble");
            unknownWords.Remove("asfloat");
            unknownWords.Remove("asint");
            unknownWords.Remove("aslong");
            unknownWords.Remove("asuint");
            unknownWords.Remove("asulong");
            unknownWords.Remove("ceillog");
            unknownWords.Remove("ceilpow");
            unknownWords.Remove("cmax");
            unknownWords.Remove("cmin");
            unknownWords.Remove("cnoise");
            unknownWords.Remove("countbits");
            unknownWords.Remove("csum");
            unknownWords.Remove("distancesq");
            unknownWords.Remove("floorlog");
            unknownWords.Remove("frac");
            unknownWords.Remove("hashwide");
            unknownWords.Remove("isfinite");
            unknownWords.Remove("isinf");
            unknownWords.Remove("ispow");
            unknownWords.Remove("lengthsq");
            unknownWords.Remove("lzcnt");
            unknownWords.Remove("modf");
            unknownWords.Remove("pnoise");
            unknownWords.Remove("psrdnoise");
            unknownWords.Remove("psrnoise");
            unknownWords.Remove("reversebits");
            unknownWords.Remove("rsqrt");
            unknownWords.Remove("snoise");
            unknownWords.Remove("srdnoise");
            unknownWords.Remove("srnoise");
            unknownWords.Remove("trunc");
            unknownWords.Remove("tzcnt");
            unknownWords.Remove("unitexp");
            unknownWords.Remove("unitlog");
            // ReSharper restore StringLiteralTypo
        }
    }
}
