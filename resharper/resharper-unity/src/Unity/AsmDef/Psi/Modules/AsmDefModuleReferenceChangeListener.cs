#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Application.changes;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Paths;
using JetBrains.ReSharper.Psi.Transactions;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using ProjectExtensions = JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.ProjectExtensions;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Modules
{
    // Listen for a module reference being added or removed, and update the source module's .asmdef file, if possible.
    // * The source module is the module that is being added to or removed from. It will always be a project, and it
    //   should have an asmdef file. All generated projects will have an asmdef file apart from the predefined
    //   Assembly-CSharp* projects, which are at the root of the dependency tree. These predefined projects
    //   automatically reference any asmdef project that has "autoReferenced" set to true.
    // * The target module is the module being added or removed. It will most likely/hopefully be an asmdef project, but
    //   could also be a predefined project (which will be an incorrect add/remove) or a plain DLL, split into external,
    //   plugin, system or engine DLLs.
    // * If we're adding/removing an asmdef project, update the "references" node in the source asmdef, or set
    //   "autoReferenced" true if source is a predefined project (notification?)
    // * Adding/removing a predefined project is not supported
    // * If we're adding/removing an engine DLL (UnityEngine* or UnityEditor*), set "noEngineReferences" to true/false
    // * A plugin DLL is a DLL that lives inside Assets or a package. By default, they're automatically referenced. If
    //   "overrideReferences" is true, the plugins need to be listed manually
    // * Adding/removing a system DLL (System* or Microsoft*) is not supported
    // * Any other DLL is external and is not supported
    //
    // * ReSharper will only add a valid module, so we won't get circular references
    // * If we have Player projects and use Alt+Enter to add a reference from a QF, the reference is only added to the
    //   current project context. We could update the other context's project?
    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class AsmDefModuleReferenceChangeListener : IChangeProvider, ISolutionLoadListenerOnDone
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly ChangeManager myChangeManager;
        private readonly AsmDefCache myAsmDefCache;
        private readonly MetaFileGuidCache myMetaFileGuidCache;
        private readonly IPsiServices myPsiServices;
        private readonly ILogger myLogger;

        public AsmDefModuleReferenceChangeListener(Lifetime lifetime, ISolution solution,
                                                   UnitySolutionTracker unitySolutionTracker,
                                                   ChangeManager changeManager,
                                                   AsmDefCache asmDefCache, MetaFileGuidCache metaFileGuidCache,
                                                   IPsiServices psiServices, ILogger logger)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myUnitySolutionTracker = unitySolutionTracker;
            myChangeManager = changeManager;
            myAsmDefCache = asmDefCache;
            myMetaFileGuidCache = metaFileGuidCache;
            myPsiServices = psiServices;
            myLogger = logger;
        }

        public async Task OnSolutionLoadDone(OuterLifetime lifetime, ISolutionLoadTasksSchedulerThreading threading)
        {
            await threading.YieldToIfNeeded(lifetime, Scheduling.MainGuard);
            myChangeManager.RegisterChangeProvider(myLifetime, this);
            myChangeManager.AddDependency(myLifetime, this, mySolution);
        }

        public object? Execute(IChangeMap changeMap)
        {
            if (!myUnitySolutionTracker.IsUnityProject.Value)
                return null;

            var changes = changeMap.GetChanges<SolutionChange>().ToList();
            if (changes.IsEmpty())
                return null;

            var collector = new ReferenceChangeCollector();
            foreach (var solutionChange in changes)
                solutionChange.Accept(collector);

            collector.RemoveProblematicChangeEvents();

            // We can't make changes to the PSI while we're in a change notification
            if (!collector.AddedReferences.IsEmpty || !collector.RemovedReferences.IsEmpty)
                myChangeManager.ExecuteAfterChange(() => HandleReferenceChanges(collector));

            return null;
        }

        private void HandleReferenceChanges(ReferenceChangeCollector collector)
        {
            // TODO: Handle multiple add/removes

            foreach (var addedReference in collector.AddedReferences)
            {
                // Note that an asmdef name will usually match the name of the project being added. The only exception
                // is for player projects. The returned addedAsmDefName here is the asmdef name/non-player project name
                var ownerProject = addedReference.OwnerModule;
                if (!ownerProject.IsValid()) return; // https://youtrack.jetbrains.com/issue/DEXP-678810
                var addedReferenceName = addedReference.Name;
                var (_, ownerAsmDefLocation) = myAsmDefCache.TryGetAsmDefLocationForProject(ownerProject.Name);
                var (addedAsmDefName, addedAsmDefLocation) = myAsmDefCache.TryGetAsmDefLocationForProject(addedReferenceName);

                switch (addedReference)
                {
                    case IProjectToProjectReference:
                        OnAddedProjectReference(ownerProject, addedReferenceName, addedAsmDefName, ownerAsmDefLocation,
                            addedAsmDefLocation);
                        break;

                    case IProjectToAssemblyReference:
                        OnAddedAssemblyReference(ownerProject, addedReferenceName, ownerAsmDefLocation);
                        break;

                    default:
                        var ownerProjectKind = GetProjectKind(ownerProject.GetSolution(), ownerProject.Name,
                            ownerAsmDefLocation);
                        if (ownerProjectKind != ProjectKind.Custom)
                        {
                            // This is an unexpected reference modification (COM? SDK? Unresolved assembly?) to either
                            // a predefined or asmdef project. This change will be lost when Unity next regenerates
                            myLogger.Warn("Unsupported reference modification: {0}", addedReference);

                            // TODO: Notify the user that a weird/unsupported reference modification has happened
                        }
                        break;
                }
            }

            foreach (var removedReference in collector.RemovedReferences)
            {
                var ownerProject = removedReference.OwnerModule;
                if (!ownerProject.IsValid()) return;
                var removedReferenceName = removedReference.Name;
                var (_, ownerAsmDefLocation) = myAsmDefCache.TryGetAsmDefLocationForProject(ownerProject.Name);
                var (removedAsmDefName, removedAsmDefLocation) = myAsmDefCache.TryGetAsmDefLocationForProject(removedReferenceName);

                switch (removedReference)
                {
                    case IProjectToProjectReference:
                        OnRemovedProjectReference(ownerProject, removedReferenceName, removedAsmDefName,
                            ownerAsmDefLocation, removedAsmDefLocation);
                        break;

                    case IProjectToAssemblyReference:
                        OnRemovedAssemblyReference(ownerProject, removedReferenceName, ownerAsmDefLocation);
                        break;

                    default:
                        var ownerProjectKind = GetProjectKind(ownerProject.GetSolution(), ownerProject.Name,
                            ownerAsmDefLocation);
                        if (ownerProjectKind != ProjectKind.Custom)
                        {
                            // This is an unexpected reference modification (COM? SDK? Unresolved assembly?) to either
                            // a predefined or asmdef project. This change will be lost when Unity next regenerates
                            myLogger.Warn("Unsupported reference modification: {0}", removedReference);

                            // TODO: Notify the user that a weird/unsupported reference modification has happened
                        }
                        break;
                }
            }
        }

        private void OnAddedProjectReference(IProject ownerProject,
                                             string addedProjectName,
                                             string addedAsmDefName,
                                             VirtualFileSystemPath ownerAsmDefLocation,
                                             VirtualFileSystemPath addedAsmDefLocation)
        {
            var solution = ownerProject.GetSolution();
            var ownerProjectKind = GetProjectKind(solution, ownerProject.Name, ownerAsmDefLocation);
            var addedProjectKind = GetProjectKind(solution, addedProjectName, addedAsmDefLocation);

            switch (ownerProjectKind, addedProjectKind)
            {
                case (ProjectKind.Custom, _):
                    // Don't care. The user is modifying a custom project. Do nothing - Unity won't regenerate the
                    // project that's being modified, which means there will be no data loss, so there's no need to
                    // notify the user. Unity will regenerate the solution, which will remove the custom project. If we
                    // want to notify the user, it would be better to notify them when initially adding the custom
                    // project.
                    myLogger.Info("{0} project {1} added as a reference to {2} project {3}. Ignoring change",
                        addedProjectKind, addedProjectName, ownerProjectKind, ownerProject.Name);
                    break;

                case (ProjectKind.RegistryPackage, _):
                    // Unsupported scenario. Registry packages don't usually have projects - they're precompiled and
                    // typically automatically added as references to all generated projects. The user can generate
                    // projects for registry packages (I wish they wouldn't), and this scenario is the user modifying
                    // that project. The change will be discarded the next time Unity regenerates packages.
                    myLogger.Warn(
                        "Unsupported reference modification. Added registry package project reference {0} to {1} project {2} will be lost when Unity regenerates projects",
                        addedProjectName, ownerProjectKind, ownerProject.Name);

                    // TODO: Notify the user
                    break;

                case (_, ProjectKind.Custom):
                    // Unsupported scenario. The user is trying to modify a generated project by adding a reference to a
                    // custom project. Changes will be lost the next time Unity regenerates the projects
                    myLogger.Warn(
                        "Unsupported reference modification. Added reference to unknown project will be lost when Unity regenerates projects");

                    // TODO: Notify user that modifying a generated project will lose changes
                    break;

                case (_, ProjectKind.Predefined):
                    // Predefined projects reference all asmdef and registry projects by default. We shouldn't be able
                    // to add a predefined project to anything other than custom, which is handled above. This scenario
                    // should never happen
                    myLogger.Warn("Adding {0} project {1} as a reference to {2} project {3}?! Weird!", addedProjectKind,
                        addedProjectName, ownerProjectKind, ownerProject.Name);
                    break;

                case (_, ProjectKind.RegistryPackage):
                    // Registry packages are usually precompiled assembly references, and automatically referenced by
                    // all asmdef/predefined projects (if `overrideReferences` is `false`/unset). These will be reported
                    // as assembly references, and most likely ignored. If the user generates projects for registry
                    // packages (please don't, there's really no need), then we'll see these assembly references
                    // converted into project references. Unless `overrideReferences` is explicitly set to true, we
                    // can ignore these notifications
                    // TODO: How to handle referencing registry packages if overrideReferences is true?
                    myLogger.Info("Adding registry package project {0} as a reference to {1} project {2}. Ignoring change",
                        addedProjectName, ownerProjectKind, ownerProject.Name);
                    return;

                case (ProjectKind.AsmDef, ProjectKind.AsmDef):
                    AddAsmDefReference(ownerProject, ownerAsmDefLocation, addedAsmDefName, addedAsmDefLocation);
                    break;

                case (ProjectKind.Predefined, ProjectKind.AsmDef):
                    // Predefined projects reference all asmdef projects by default, so the asmdef project should
                    // already be referenced. However, the asmdef project might have set `autoReferenced` to false,
                    // in which case, it won't be automatically referenced, and the owning project needs to explicitly
                    // reference it.
                    // TODO: How should we reference a non-autoReferenced asmdef?
                    // Set autoReferenced to true? For UnityEngine.TestRunner, we might need to add "TestAssemblies" to
                    // optionalUnityReferences
                    myLogger.Info(
                        "Adding asmdef reference to predefined project. Should already be referenced. Check 'autoReferenced' value in asmdef");
                    break;
            }
        }

        private void AddAsmDefReference(IProject ownerProject, VirtualFileSystemPath ownerAsmDefLocation,
                                        string addedAsmDefName, VirtualFileSystemPath addedAsmDefLocation)
        {
            var sourceFile = ownerProject.GetPsiSourceFileInProject(ownerAsmDefLocation);
            if (sourceFile?.GetDominantPsiFile<JsonNewLanguage>() is not IJsonNewFile psiFile)
                return;

            var rootObject = psiFile.GetRootObject();
            if (rootObject == null)
                return;

            // If the location of the added asmdef is under Library/PackageCache, it's from a read only, registry
            // package. If the current project's `overrideReferences` is set to the default `false`, then the registry
            // package is automatically referenced by the current project. We ignore any such notification because we
            // don't support adding asmdef references to arbitrary assemblies. But if the user has enabled generation of
            // projects for registry packages, then we'll get a notification of a project to project reference, where
            // the target/referenced project should be a precompiled assembly.
            // So, if the added asmdef is under Library/PackageCache, and the current asmdef has the default value for
            // `overrideReferences`, do not add the reference to the asmdef.
            // Yet another reason to hate the "generate registry packages" setting
            // TODO: How do we add a reference explicitly? Is it by asset GUID or assembly name?

            if (addedAsmDefLocation.StartsWith(ownerProject.GetSolution().SolutionDirectory
                    .Combine("Library/PackageCache")))
            {
                // TODO: We should check if overrideReferences is set to true, and figure out what to do in this case
                // Add a reference to
                myLogger.Info("Adding");
            }

            var guid = myMetaFileGuidCache.GetAssetGuid(addedAsmDefLocation);
            if (guid == null)
                myLogger.Warn("Cannot find asset GUID for added asmdef {0}! Can only add as name", addedAsmDefLocation);
            var addedAsmDefGuid = guid == null ? null : AsmDefUtils.FormatGuidReference(guid.Value);

            var referencesProperty = rootObject.GetFirstPropertyValue<IJsonNewArray>("references");
            var existingArrayElement =
                FindReferenceElement(referencesProperty, addedAsmDefName, addedAsmDefGuid, out var useGuids);
            if (existingArrayElement.Count != 0)
            {
                myLogger.Verbose("Reference {0} already exists in asmdef {1}", addedAsmDefName, ownerAsmDefLocation);
                return;
            }

            myLogger.Info("Adding reference {0} to asmdef {1}", addedAsmDefName, ownerAsmDefLocation);

            using (new PsiTransactionCookie(myPsiServices, DefaultAction.Commit, "AddAsmDefReference"))
            {
                var referenceText = useGuids && addedAsmDefGuid != null ? addedAsmDefGuid : addedAsmDefName;
                var elementFactory = JsonNewElementFactory.GetInstance(psiFile.GetPsiModule());
                if (referencesProperty == null)
                {
                    referencesProperty =
                        (IJsonNewArray)elementFactory.CreateValue($"[ \"{referenceText}\" ]");
                    rootObject.AddMemberBefore("references", referencesProperty, null);
                }
                else
                {
                    var reference = elementFactory.CreateStringLiteral(referenceText);
                    referencesProperty.AddArrayElementBefore(reference, null);
                }
            }
        }

        private void OnAddedAssemblyReference(IProject ownerProject, string addedAssemblyName,
                                              VirtualFileSystemPath ownerAsmDefLocation)
        {
            var ownerProjectKind = GetProjectKind(ownerProject.GetSolution(), ownerProject.Name, ownerAsmDefLocation);

            switch (ownerProjectKind)
            {
                case ProjectKind.Custom:
                    // Don't care. The user has added a custom project, and has added a reference to it. The project
                    // will be removed the next time Unity regenerates the solution, but they won't lose the changes to
                    // the project. We don't notify about this action - it would be better to notify when the project is
                    // initially added
                    myLogger.Info("Adding assembly {0} to custom project {1}. Ignoring change", addedAssemblyName,
                        ownerProject.Name);
                    break;

                case ProjectKind.RegistryPackage:
                    // The user has generated projects for registry packages and has added a reference to it. This will
                    // get removed the next time Unity regenerates the solution.
                    // TODO: Notify the user
                    myLogger.Warn(
                        "Unsupported reference modification. Added assembly reference {0} to registry package project {1} will be lost when Unity regenerates project files",
                        addedAssemblyName, ownerProject.Name);
                    break;

                case ProjectKind.Predefined:
                    // Unsupported scenario. Custom assemblies should live in Assets and requires regenerating the
                    // project file
                    // TODO: Support adding assemblies to predefined projects
                    myLogger.Warn(
                        "Unsupported reference modification. Added assembly reference {0} to predefined project {1} will be lost when Unity regenerates project files",
                        addedAssemblyName, ownerProject.Name);

                    // TODO: Notify the user
                    break;

                case ProjectKind.AsmDef:
                    // AsmDef can only reference custom assemblies if that assembly is a plugin (i.e. it's an asset,
                    // either in Assets or a package). By default, all asmdef projects will reference all plugin
                    // assemblies. An asmdef can opt out by setting "overrideReferences" to true and listing each
                    // plugin individually.
                    // This is a complex scenario:
                    // 1. Check addedAssembly is a plugin
                    // 2. If not, notify user that this is not supported
                    // 3. If true, check "overrideReferences" (if false, this is a weird scenario)
                    // 4. If true, add assembly name to "precompiledReferences"
                    // It might be easier to notify the user to edit assembly references using Unity
                    // TODO: All of the above ^^

                    myLogger.Warn(
                        "Unsupported reference modification. Added assembly reference {0} to asmdef based project {1} will be lost when Unity regenerates project files",
                        addedAssemblyName, ownerProject.Name);
                    break;
            }
        }

        private void OnRemovedProjectReference(IProject ownerProject,
                                               string removedProjectName,
                                               string removedAsmDefName,
                                               VirtualFileSystemPath ownerAsmDefLocation,
                                               VirtualFileSystemPath removedAsmDefLocation)
        {
            var solution = ownerProject.GetSolution();
            var ownerProjectKind = GetProjectKind(solution, ownerProject.Name, ownerAsmDefLocation);
            var removedProjectKind = GetProjectKind(solution, removedProjectName, removedAsmDefLocation);

            switch (ownerProjectKind, removedProjectKind)
            {
                case (ProjectKind.Custom, _):
                    // The user is modifying a custom project. Do nothing - Unity won't regenerate the project that's
                    // being modified, so there will be no data loss, so there's no need to notify the user. Unity will
                    // regenerate the solution, which will remove the custom project. If we want to notify the user,
                    // it's best to notify them when initially adding the custom project.
                    myLogger.Info("{0} project {1} removed as a reference from {2} project {3}. Ignoring change",
                        removedProjectKind, removedProjectName, ownerProjectKind, ownerProject.Name);
                    break;

                case (ProjectKind.RegistryPackage, _):
                    // Unsupported scenario. The user is trying to modify a registry package project. They shouldn't do
                    // this
                    myLogger.Warn(
                        "Unsupported reference modification. Removed assembly reference {0} from registry package project {1}. Change will be lost when Unity regenerates project files",
                        removedProjectName, ownerProject.Name);

                    // TODO: Notify user that modifying a generated project will lose changes
                    break;

                case (_, ProjectKind.Custom):
                    // Unsupported scenario. The user is trying to modify a generated project by adding a reference to a
                    // custom project.
                    myLogger.Warn(
                        "Unsupported reference modification. Removed assembly reference {0} from project {1} will be lost when Unity regenerates project files",
                        removedProjectName, ownerProject.Name);

                    // TODO: Notify user that modifying a generated project will lose changes
                    break;

                case (_, ProjectKind.Predefined):
                    // Removing a reference to a predefined project is weird. Nothing should be referencing them
                    myLogger.Warn("Removing {0} project {1} reference from {2} project {3}?! Weird!",
                        removedProjectKind, removedProjectName, ownerProjectKind, ownerProject.Name);
                    break;

                case (ProjectKind.AsmDef, ProjectKind.AsmDef):
                    RemoveAsmDefReference(ownerProject, ownerAsmDefLocation, removedAsmDefName, removedAsmDefLocation);
                    break;

                case (ProjectKind.Predefined, ProjectKind.AsmDef):
                    myLogger.Info(
                        "Removing asmdef reference {0} from predefined project {1}. Check 'autoReferenced' value in asmdef",
                        removedProjectName, ownerProject.Name);

                    // TODO: Should set "autoReferenced" to false in the removed asmdef project. Should this prompt?
                    break;
            }
        }

        private void RemoveAsmDefReference(IProject ownerProject, VirtualFileSystemPath ownerAsmDefLocation,
                                           string removedAsmDefName, VirtualFileSystemPath removedAsmDefLocation)
        {
            var sourceFile = ownerProject.GetPsiSourceFileInProject(ownerAsmDefLocation);
            var psiFile = sourceFile?.GetDominantPsiFile<JsonNewLanguage>() as IJsonNewFile;
            var rootObject = psiFile?.GetRootObject();
            if (rootObject == null)
                return;

            var guid = myMetaFileGuidCache.GetAssetGuid(removedAsmDefLocation);
            if (guid == null)
                myLogger.Warn("Cannot find asset GUID for removed asmdef {0}!", removedAsmDefLocation);
            var removedAsmDefGuid = guid == null ? null : AsmDefUtils.FormatGuidReference(guid.Value);

            // "references" might be missing if we've already removed all references and Unity isn't running to refresh
            // the project files
            var referencesProperty = rootObject.GetFirstPropertyValue<IJsonNewArray>("references");
            if (referencesProperty == null)
            {
                myLogger.Verbose("Cannot find 'references' property in {0}. Nothing to remove", ownerAsmDefLocation);
                return;
            }

            var existingArrayElement =
                FindReferenceElement(referencesProperty, removedAsmDefName, removedAsmDefGuid, out _);
            if (existingArrayElement.Count == 0)
            {
                myLogger.Verbose("Reference {0} already removed from asmdef {1}", removedAsmDefName,
                    ownerAsmDefLocation);
                return;
            }

            using (new PsiTransactionCookie(myPsiServices, DefaultAction.Commit, "AddAsmDefReference"))
            {
                foreach (var existingLiteral in existingArrayElement)
                    referencesProperty.RemoveArrayElement(existingLiteral);
            }
        }

        private void OnRemovedAssemblyReference(IProject ownerProject, string removedAssemblyName,
                                                VirtualFileSystemPath ownerAsmDefLocation)
        {
            var ownerProjectKind = GetProjectKind(ownerProject.GetSolution(), ownerProject.Name, ownerAsmDefLocation);

            switch (ownerProjectKind)
            {
                case ProjectKind.Custom:
                    // Don't care. The user has added a custom project, and has removed a reference from it. The project
                    // will be removed the next time Unity regenerates the solution, but they won't lose the changes to
                    // the project. We don't notify about this action - it would be better to notify when the project is
                    // initially added
                    myLogger.Info("Removing assembly {0} to custom project {1}. Ignoring change", removedAssemblyName,
                        ownerProject.Name);
                    return;

                case ProjectKind.RegistryPackage:
                    // Unsupported scenario. User is modifying a project generated for a read only registry package.
                    // They shouldn't be modifying it, and changes will be lost when Unity regenerates projects.
                    myLogger.Warn(
                        "Unsupported reference modification. Removing assembly {0} from registry package project {1}. Change will be lost when Unity regenerates project files",
                        removedAssemblyName, ownerProject.Name);

                    // TODO: Notify the  user
                    return;

                case ProjectKind.Predefined:
                    // Unsupported scenario. Custom assemblies should live in Assets and requires regenerating the
                    // project file
                    myLogger.Warn(
                        "Unsupported reference modification. Removing assembly {0} from project {1}. Change will be lost when Unity regenerates project files",
                        removedAssemblyName, ownerProject.Name);

                    // TODO: Notify the user
                    return;

                case ProjectKind.AsmDef:
                    // Like adding an assembly reference to an asmdef project, removing one is only supported if we're
                    // removing an explicit plugin reference.
                    // 1. Check removedAssembly is a plugin
                    // 2. If not, notify user that this is not supported
                    // 3. If true, check "overrideReferences" (if false, this is a weird scenario)
                    // 4. If true, remove assembly name from "precompiledReferences"
                    // It might be easier to notify the user to edit assembly references using Unity
                    // TODO: All of the above ^^

                    myLogger.Warn(
                        "Unsupported reference modification. Removing assembly {0} from project {1}. Change will be lost when Unity regenerates project files",
                        removedAssemblyName, ownerProject.Name);
                    break;
            }
        }

        private static ProjectKind GetProjectKind(ISolution solution, string projectName,
                                                  VirtualFileSystemPath asmDefLocation)
        {
            if (asmDefLocation.IsNotEmpty)
            {
                if (asmDefLocation.StartsWith(solution.SolutionDirectory.Combine("Library/PackageCache")))
                    return ProjectKind.RegistryPackage;
                return ProjectKind.AsmDef;
            }

            if (ProjectExtensions.IsOneOfPredefinedUnityProjects(projectName, true)) return ProjectKind.Predefined;
            return ProjectKind.Custom;
        }

        private static FrugalLocalList<IJsonNewValue> FindReferenceElement(IJsonNewArray? array, string asmDefName,
                                                                           string? asmDefGuid, out bool useGuids)
        {
            useGuids = false;
            var count = 0;
            var guidCount = 0;

            // The user might have added more than one, even though that's an error
            var results = new FrugalLocalList<IJsonNewValue>();
            foreach (var literal in array.ValuesAsLiteral())
            {
                // TODO: Helper function in JsonNewUtil that doesn't allocate?
                var text = literal.GetUnquotedText();
                if (text.Equals(asmDefName, StringComparison.OrdinalIgnoreCase))
                    results.Add(literal);
                else if (asmDefGuid != null && text.Equals(asmDefGuid, StringComparison.OrdinalIgnoreCase))
                    results.Add(literal);

                count++;
                if (AsmDefUtils.IsGuidReference(text))
                    guidCount++;
            }

            // Prefer GUIDs, unless everything else is non-guid
            if (count == 0 || guidCount > 0)
                useGuids = true;

            return results;
        }

        private class ReferenceChangeCollector : RecursiveProjectModelChangeDeltaVisitor
        {
            public FrugalLocalList<IProjectToModuleReference> AddedReferences;
            public FrugalLocalList<IProjectToModuleReference> RemovedReferences;

            public override void VisitProjectReferenceDelta(ProjectReferenceChange change)
            {
                if (change.IsClosingSolution || change.IsOpeningSolution)
                    return;

                if (change.IsAdded)
                    AddedReferences.Add(change.ProjectToModuleReference);

                // TODO: Safely handle a removed reference
                // False positives when adding a reference are safe - we won't get a notification unless the reference
                // actually has been added, in which case it's always safe to modify the .asmdef file.
                // Removing references is different, and is not safe. If we get a notification for anything other than
                // explicitly removing a reference, we'll incorrectly modify the .asmdef file. And we can get into this
                // scenario easily:
                // 1) A referenced project is unloaded. As far as the project model is concerned, this is the same as
                //    removing a reference, even though it still exists in the project file. We would need to check that
                //    the project being referenced has been unloaded instead of removed, and I don't know if there's an
                //    API for that.
                // 2) If a .asmdef file is disabled, e.g. by adding an unmet constraint in defineConstraints, then Unity
                //    will generate an "empty" project file - no C# files, minimal references and various other files
                //    (.txt, .asmdef, .shader, ...). If the modification happens when the project is open, reloading
                //    project file will trigger reference remove notifications, which are valid, because the references
                //    no longer exist in the project file. We could check to see if the referencing project has any C#
                //    files. If yes, then the .asmdef file is valid and it (should be) a genuine reference removal. If
                //    no, then it's an "empty" project and we should not edit the .asmdef file.
                //    (We could also change the project generation to not generate the "empty" project. It's only
                //    generated so that .shader files work. We could add any .shader file to the external files project.
                //    But we'd still have to support the current version, so we'd still have to handle "empty" projects)
                // It's too late in the 221 cycle to fix this, so let's disable removing references from .asmdef for now
                //
                // else if (change.IsRemoved)
                    // RemovedReferences.Add(change.ProjectToModuleReference);
            }

            public void RemoveProblematicChangeEvents()
            {
                // We will be notified of reference changes when projects are reloaded after Unity regenerates the
                // project files. If we added a reference to a .Player project (the "Add reference" QF might arbitrarily
                // pick this) then Unity will regenerate the project files with the correct reference, and we'll see
                // both a removal of the .Player and an addition of the proper project. Since we always add the correct
                // asmdef reference even for .Player references, make sure we don't process this removal!
                // We could also post process this list to remove any notifications for .Player projects if there are
                // also notifications for non-player projects.
                var toRemove = new FrugalLocalList<IProjectToModuleReference>();
                foreach (var removedReference in RemovedReferences)
                {
                    if (ProjectExtensions.IsPlayerProjectName(removedReference.Name))
                    {
                        var nonPlayerProjectName = ProjectExtensions.StripPlayerSuffix(removedReference.Name);
                        foreach (var addedReference in AddedReferences)
                        {
                            if (addedReference.Name.Equals(nonPlayerProjectName, StringComparison.OrdinalIgnoreCase))
                                toRemove.Add(removedReference);
                        }
                    }
                }

                foreach (var reference in toRemove)
                    RemovedReferences.Remove(reference);
            }
        }

        private enum ProjectKind
        {
            Predefined,
            AsmDef,
            RegistryPackage,
            Custom
        }
    }
}
