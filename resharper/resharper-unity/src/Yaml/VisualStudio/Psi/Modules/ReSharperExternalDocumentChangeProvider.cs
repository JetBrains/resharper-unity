using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.DocumentManagers;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.Impl;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.VisualStudio.Psi.Modules
{
    // When an external file is opened in VS through e.g. drag/drop, it's added to the Misc Files project and we get a
    // project file. Our IMiscFilesProjectPsiModuleProvider can provide the associated IPsiSourceFile and document
    // change notifications propagate correctly to the caches (see PsiCaches.Execute).
    // If the external file is opened via Solution Explorer + Show All Files, then it isn't added to Misc Files, so
    // there isn't a project file. When the document is changed, there is no project file to provide an IPsiSourceFile,
    // so document change notifications don't get to the caches.
    // This change provider converts document change notifications to PSI source notifications for our external files
    // that do not have project files.
    // This is not required in Rider because we always create a project file (so there's always an association between
    // document, project file and PSI source file)
    // (See also SolutionDocumentChangeProvider)
    [SolutionComponent]
    public class ReSharperExternalDocumentChangeProvider : IChangeProvider
    {
        public ReSharperExternalDocumentChangeProvider(Lifetime lifetime,
                                                       ChangeManager changeManager,
                                                       DocumentChangeManager documentChangeManager,
                                                       DocumentToProjectFileMappingStorage documentToProjectFileMapping,
                                                       ITextControlManager textControlManager,
                                                       IPsiModules psiModules,
                                                       IPsiCaches psiCaches,
                                                       UnityExternalFilesModuleFactory moduleFactory)
        {
            // We want to listen to changes published by DocumentChangeManager, and publish our changes to PsiCaches.
            // However, PsiCaches only handles PsiModuleChanges published by PsiModules, so we have to publish to
            // PsiModules, which will fortunately combine our changes with its own.
            // Registering the DocumentChangeManager->this->PsiModules dependencies ends up being quite intrusive,
            // adding a large sub-graph of PsiModules and all of its dependencies into the change graph. While it is
            // safe (any changes coming through us from DocumentChangeManager will be ignored by the new sub-graph), it
            // is extra processing that is not required for non-Unity projects, and the majority of files in a Unity
            // project. So, let's listen for DocumentChangeManager changes via the Changed2 event, and publish them
            // separately. We still set up a dependency this->PsiModules so that our published changes are handled by
            // PsiModules
            changeManager.RegisterChangeProvider(lifetime, this);
            changeManager.AddDependency(lifetime, psiModules, this);

            changeManager.Changed2.Advise(lifetime, args =>
            {
                var changeMap = args.ChangeMap;

                var module = moduleFactory.PsiModule;
                if (module == null) return;

                var documentChange = changeMap.GetChange<DocumentChange>(documentChangeManager);
                if (documentChange == null)
                {
                    var change = changeMap.GetChange<DocumentCopyChanged>(documentChangeManager);
                    if (change == null) return;

                    documentChange = change.DocumentChange;
                }

                var document = documentChange.Document;
                var projectFile = documentToProjectFileMapping.TryGetProjectFile(document);
                if (projectFile != null) return;

                // No project file. Is it one of our interesting assets?
                var location = document.TryGetFilePath();
                if (location.IsEmpty) return;

                if (location.IsInterestingAsset() || location.IsMeta())
                {
                    if (module.TryGetFileByPath(location, out var psiSourceFile)
                        && psiSourceFile is UnityYamlExternalPsiSourceFile sourceFile)
                    {
                        // Only process the changes if the document is open. If it's closed, then it will be saved to
                        // disk when the transaction commits, and the file system watcher will report the change
                        if (textControlManager.TextControls.All(tc => tc.Document != document))
                            return;

                        // PsiCaches would call IPsiSourceFileCache.OnDocumentChanged => ICache.MarkAsDirty, but only
                        // in response to a ProjectFileDocumentChange
                        psiCaches.MarkAsDirty(sourceFile);

                        // Tell the source file that the document has changed, or the cache thinks it's up to date
                        sourceFile.MarkDocumentModified();

                        var builder = new PsiModuleChangeBuilder();
                        builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Modified);

                        changeManager.ExecuteAfterChange(() =>
                            changeManager.OnProviderChanged(this, builder.Result, SimpleTaskExecutor.Instance));
                    }
                }
            });
        }

        public object Execute(IChangeMap changeMap) => null;
    }
}