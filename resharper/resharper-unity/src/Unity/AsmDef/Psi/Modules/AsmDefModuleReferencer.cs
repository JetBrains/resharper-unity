#nullable enable

using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Paths;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Modules
{
    // Intercepts "Add reference" for Unity AsmDef projects, writes directly to the .asmdef file and returns false to let the .csproj modification to also happen.
    [ModuleReferencer(Priority = -10)]
    public class AsmDefModuleReferencer(ILogger logger) : IModuleReferencer
    {
        public bool CanReferenceModule(IPsiModule module, IPsiModule? moduleToReference, UserDataHolder? context)
        {
            return CanReferenceModule(module, moduleToReference);
        }

        private static bool CanReferenceModule(IPsiModule module, IPsiModule? moduleToReference)
        {
            if (module is not IProjectPsiModule sourceModule ||
                moduleToReference is not IProjectPsiModule targetModule)
                return false;

            var solution = sourceModule.Project.GetSolution();
            var asmDefCache = solution.GetComponent<AsmDefCache>();

            return IsEditableAsmDefProject(asmDefCache, solution, sourceModule.Project.Name)
                   && IsEditableAsmDefProject(asmDefCache, solution, targetModule.Project.Name);
        }

        public bool ReferenceModule(IPsiModule module, IPsiModule moduleToReference)
        {
            if (!CanReferenceModule(module, moduleToReference))
                return false;
            
            if (module is not IProjectPsiModule sourceModule ||
                moduleToReference is not IProjectPsiModule targetModule)
                return false;

            var solution = sourceModule.Project.GetSolution();
            var asmDefCache = solution.GetComponent<AsmDefCache>();

            var (_, ownerAsmDefLocation) = asmDefCache.TryGetAsmDefLocationForProject(sourceModule.Project.Name);
            var (addedAsmDefName, addedAsmDefLocation) = asmDefCache.TryGetAsmDefLocationForProject(targetModule.Project.Name);

            if (ownerAsmDefLocation.IsEmpty || addedAsmDefLocation.IsEmpty)
                return false;

            var metaFileGuidCache = solution.GetComponent<MetaFileGuidCache>();
            var result = AddAsmDefReference(sourceModule.Project, ownerAsmDefLocation, addedAsmDefName, addedAsmDefLocation,
                metaFileGuidCache, module.GetPsiServices());
            
            logger.Info($"Adding reference {addedAsmDefName} to asmdef {ownerAsmDefLocation} => {result}");
            
            // After the reference is added, we return false, so that the default reference handling is also invoked to modify the .csproj file.
            // Otherwise, when Unity is not running, it would seem that the reference was not added, even though it was.
            return false;
        }

        public bool ReferenceModuleWithType(IPsiModule module, ITypeElement typeToReference)
        {
            return ReferenceModule(module, typeToReference.Module);
        }

        private static bool IsEditableAsmDefProject(AsmDefCache asmDefCache, ISolution solution, string projectName)
        {
            var (_, location) = asmDefCache.TryGetAsmDefLocationForProject(projectName);
            if (location.IsEmpty)
                return false;

            // Registry packages under Library/PackageCache are read-only; do not attempt to modify them
            return !location.StartsWith(solution.SolutionDirectory.Combine("Library/PackageCache"));
        }

        private bool AddAsmDefReference(IProject ownerProject, VirtualFileSystemPath ownerAsmDefLocation,
                                        string addedAsmDefName, VirtualFileSystemPath addedAsmDefLocation,
                                        MetaFileGuidCache metaFileGuidCache, IPsiServices psiServices)
        {
            var sourceFile = ownerProject.GetPsiSourceFileInProject(ownerAsmDefLocation);
            if (sourceFile?.GetDominantPsiFile<JsonNewLanguage>() is not IJsonNewFile psiFile)
                return false;

            var rootObject = psiFile.GetRootObject();
            if (rootObject == null)
                return false;

            var guid = metaFileGuidCache.GetAssetGuid(addedAsmDefLocation);
            if (guid == null)
                logger.Warn($"Cannot find asset GUID for added asmdef {addedAsmDefLocation}! Can only add as name");
            var addedAsmDefGuid = guid == null ? null : AsmDefUtils.FormatGuidReference(guid.Value);

            var referencesProperty = rootObject.GetFirstPropertyValue<IJsonNewArray>("references");
            var existingArrayElement = FindReferenceElement(referencesProperty, addedAsmDefName, addedAsmDefGuid, out var useGuids);
            if (existingArrayElement.Count != 0)
            {
                logger.Verbose($"Reference {addedAsmDefName} already exists in asmdef {ownerAsmDefLocation}");
                return true;
            }

            logger.Info($"Adding reference {addedAsmDefName} to asmdef {ownerAsmDefLocation}");

            using (new PsiTransactionCookie(psiServices, DefaultAction.Commit, "AddAsmDefReference"))
            {
                var referenceText = useGuids && addedAsmDefGuid != null ? addedAsmDefGuid : addedAsmDefName;
                var elementFactory = JsonNewElementFactory.GetInstance(psiFile.GetPsiModule());
                if (referencesProperty == null)
                {
                    referencesProperty = (IJsonNewArray)elementFactory.CreateValue($"[ \"{referenceText}\" ]");
                    rootObject.AddMemberBefore("references", referencesProperty, null);
                }
                else
                {
                    var reference = elementFactory.CreateStringLiteral(referenceText);
                    referencesProperty.AddArrayElementBefore(reference, null);
                }
            }

            return true;
        }

        private static FrugalLocalList<IJsonNewValue> FindReferenceElement(IJsonNewArray? array, string asmDefName,
                                                                           string? asmDefGuid, out bool useGuids)
        {
            useGuids = false;
            var count = 0;
            var guidCount = 0;
            var results = new FrugalLocalList<IJsonNewValue>();

            foreach (var literal in array.ValuesAsLiteral())
            {
                var text = literal.GetUnquotedText();
                if (text.Equals(asmDefName, StringComparison.OrdinalIgnoreCase) || asmDefGuid != null && text.Equals(asmDefGuid, StringComparison.OrdinalIgnoreCase))
                    results.Add(literal);

                count++;
                if (AsmDefUtils.IsGuidReference(text))
                    guidCount++;
            }

            // Prefer GUIDs unless everything else is non-GUID
            if (count == 0 || guidCount > 0)
                useGuids = true;

            return results;
        }
    }
}
