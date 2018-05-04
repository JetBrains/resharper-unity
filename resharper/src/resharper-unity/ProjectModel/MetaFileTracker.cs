using System;
using System.IO;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.DataFlow;
using JetBrains.DocumentManagers.Transactions;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionComponent]
    public class MetaFileTracker : IChangeProvider
    {
        private static readonly DateTime ourUnixTime = new DateTime(1970, 1, 1, 0, 0, 0);

        private readonly ISolution mySolution;
        private readonly ILogger myLogger;
        private IProjectItem myLastAddedItem;

        public MetaFileTracker(Lifetime lifetime, ChangeManager changeManager, ISolution solution, ILogger logger, ISolutionLoadTasksScheduler solutionLoadTasksScheduler)
        {
            mySolution = solution;
            myLogger = logger;
            
            solutionLoadTasksScheduler.EnqueueTask(new SolutionLoadTask("AdviseForChanges", SolutionLoadTaskKinds.AfterDone,
                () =>
                {
                    changeManager.RegisterChangeProvider(lifetime, this);
                    changeManager.AddDependency(lifetime, this, solution);        
                }));
        }

        public object Execute(IChangeMap changeMap)
        {
            var projectModelChange = changeMap.GetChange<ProjectModelChange>(mySolution);
            if (projectModelChange == null) return null;
            if (projectModelChange.IsOpeningSolution || projectModelChange.IsClosingSolution)
                return null;

            try
            {
                projectModelChange.Accept(new Visitor(this, myLogger));
            }
            catch (Exception e)
            {
                using (var sw = new StringWriter())
                {
                    projectModelChange.Dump(sw);
                    myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.Algorithmic,
                        $"Unity::MetaFileTracker - Error processing project model change{Environment.NewLine}{sw}");
                }
            }
            return null;
        }

        private class Visitor : RecursiveProjectModelChangeDeltaVisitor
        {
            private readonly MetaFileTracker myMetaFileTracker;
            private readonly ILogger myLogger;

            public Visitor(MetaFileTracker metaFileTracker, ILogger logger)
            {
                myMetaFileTracker = metaFileTracker;
                myLogger = logger;
            }

            // Note that this method is called recursively, for projects, folders and files
            public override void VisitItemDelta(ProjectItemChange change)
            {
                if (change.ProjectModelElement is IProject project)
                    VisitProjectDelta(change, project);
                else
                    VisitFileOrFolderDelta(change);
            }

            private void VisitProjectDelta(ProjectItemChange projectChange, IProject project)
            {
                // When a project is reloaded, we get a removal notification for it and all of its
                // files, followed by a load of addition notifications. If we don't handle this
                // properly, we'll delete a load of .meta files and create new ones, causing big problems.
                // So ignore any project removal messages. We can safely ignore them, as a) Unity will
                // never do this, and b) you can't delete a project from Visual Studio/Rider, only remove it
                if (projectChange.IsRemoved)
                    return;

                // Don't recurse if this project isn't a Unity project. Note that we don't do this
                // for the IsRemoved case above, as the project doesn't have a solution at that point,
                // and IsUnityProject will throw
                if (!project.IsUnityProject())
                    return;

                base.VisitItemDelta(projectChange);
            }

            private void VisitFileOrFolderDelta(ProjectItemChange fileOrFolderChange)
            {
                var shouldVisitChildren = true;
                if (IsRenamedAsset(fileOrFolderChange))
                    shouldVisitChildren = OnItemRenamed(fileOrFolderChange);
                else if (IsAddedAsset(fileOrFolderChange))
                    OnItemAdded(fileOrFolderChange);
                else if (IsRemovedAsset(fileOrFolderChange))
                    OnItemRemoved(fileOrFolderChange);

                if (shouldVisitChildren)
                    base.VisitItemDelta(fileOrFolderChange);
            }

            private static bool IsRenamedAsset(ProjectItemChange change)
            {
                return change.IsMovedOut && ShouldHandleChange(change);
            }

            private bool IsAddedAsset(ProjectItemChange change)
            {
                if (change.IsAdded && ShouldHandleChange(change))
                    return true;
                return change.IsContentsExternallyChanged && myMetaFileTracker.myLastAddedItem != null &&
                       myMetaFileTracker.myLastAddedItem.Location == change.ProjectItem.Location &&
                       ShouldHandleChange(change);
            }

            private static bool IsRemovedAsset(ProjectItemChange change)
            {
                return change.IsRemoved && ShouldHandleChange(change);
            }

            private static bool ShouldHandleChange(ProjectItemChange change)
            {
                // String comparisons, treat as expensive if we're doing this very frequently
                return IsAsset(change) && !IsItemMetaFile(change);
            }

            private static bool IsAsset(ProjectItemChange change)
            {
                var rootFolder = GetRootFolder(change.OldParentFolder);
                return rootFolder != null && string.Compare(rootFolder.Name, ProjectExtensions.AssetsFolder, StringComparison.OrdinalIgnoreCase) == 0;
            }

            private static IProjectFolder GetRootFolder(IProjectItem item)
            {
                while (item?.ParentFolder != null && item.ParentFolder.Kind != ProjectItemKind.PROJECT)
                    item = item.ParentFolder;
                return item as IProjectFolder;
            }

            private static bool IsItemMetaFile(ProjectItemChange change)
            {
                return change.ProjectItem.Name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
            }

            private bool OnItemRenamed(ProjectItemChange change)
            {
                myLogger.Trace("*** resharper-unity: Item renamed {0} -> {1}", change.OldLocation, change.ProjectItem.Location);

                var newMetaFile = GetMetaFile(change.ProjectItem.Location);
                if (!newMetaFile.ExistsFile)
                {
                    var oldMetaFile = change.OldParentFolder.Location.Combine(change.OldLocation.Name + ".meta");
                    if (newMetaFile != oldMetaFile)
                    {
                        if (oldMetaFile.ExistsFile)
                            RenameMetaFile(oldMetaFile, newMetaFile, string.Empty);
                        else
                            CreateMetaFile(newMetaFile);
                    }
                }

                // Don't recurse for folder renames - the child contents will be "renamed", but
                // the old location will no longer be there, and the meta files don't need moving
                return !(change.ProjectItem is IProjectFolder);
            }

            private void OnItemAdded(ProjectItemChange change)
            {
                myLogger.Trace("*** resharper-unity: Item added {0}", change.ProjectItem.Location);

                CreateMetaFile(GetMetaFile(change.ProjectItem.Location));

                // We sometimes get notified of an item being added when it's not actually on disk.
                // This appears to be happen:
                // 1) When loading a project, with a stale cache (it gets removed once loaded)
                // 2) When invoking the Move To Folder refactoring on a type. ReSharper adds a new file
                //    and then removes the existing file, but only if it's empty. We create a new meta
                //    file, and overwrite it with the existing one if the empty file is removed.
                myMetaFileTracker.myLastAddedItem = null;
                if (change.ProjectItem.Location.Exists == FileSystemPath.Existence.Missing)
                    myMetaFileTracker.myLastAddedItem = change.ProjectItem;
            }

            private void OnItemRemoved(ProjectItemChange change)
            {
                myLogger.Trace("*** resharper-unity: Item removed {0}", change.OldLocation);

                // Only delete the meta file if the original file or folder is missing
                if (change.OldLocation.Exists != FileSystemPath.Existence.Missing)
                    return;

                var metaFile = GetMetaFile(change.OldLocation);
                if (!metaFile.ExistsFile)
                    return;

                if (IsMoveToFolderRefactoring(change))
                {
                    var newMetaFile = GetMetaFile(myMetaFileTracker.myLastAddedItem.Location);
                    RenameMetaFile(metaFile, newMetaFile, " via add/remove");
                }
                else
                    DeleteMetaFile(metaFile);

                myMetaFileTracker.myLastAddedItem = null;
            }

            private bool IsMoveToFolderRefactoring(ProjectItemChange change)
            {
                // Adding to one folder and removing from another is the Move To Folder refactoring. If
                // we get a remove event, that means the original file is empty, and we should treat it
                // like a rename
                return myMetaFileTracker.myLastAddedItem != null &&
                       myMetaFileTracker.myLastAddedItem.Name == change.OldLocation.Name &&
                       myMetaFileTracker.myLastAddedItem.Location != change.OldLocation;
            }

            private static FileSystemPath GetMetaFile(FileSystemPath location)
            {
                return FileSystemPath.Parse(location + ".meta");
            }

            private void CreateMetaFile(FileSystemPath path)
            {
                if (path.ExistsFile)
                    return;

                try
                {
                    var guid = Guid.NewGuid();
                    var timestamp = (long)(DateTime.UtcNow - ourUnixTime).TotalSeconds;
                    DoUnderTransaction("Unity::CreateMetaFile", () => path.WriteAllText($"fileFormatVersion: 2\r\nguid: {guid:N}\r\ntimeCreated: {timestamp}"));
                    myLogger.Info("*** resharper-unity: Meta added {0}", path);
                }
                catch (Exception e)
                {
                    myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.Assertion,
                        $"Failed to create Unity meta file {path}");
                }
            }

            private void RenameMetaFile(FileSystemPath oldPath, FileSystemPath newPath, string extraDetails)
            {
                try
                {
                    myLogger.Info("*** resharper-unity: Meta renamed{2} {0} -> {1}", oldPath, newPath, extraDetails);
                    DoUnderTransaction("Unity::RenameMetaFile", () => oldPath.MoveFile(newPath, true));
                }
                catch (Exception e)
                {
                    myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.Assertion,
                        $"Failed to rename Unity meta file {oldPath} -> {newPath}");
                }
            }

            private void DeleteMetaFile(FileSystemPath path)
            {
                try
                {
                    if (path.ExistsFile)
                    {
                        DoUnderTransaction("Unity::DeleteMetaFile", () =>
                        {
#if DEBUG
                            path.MoveFile(FileSystemPath.Parse(path + ".deleted"), true);
#else
                            path.DeleteFile();
#endif
                        });
                        myLogger.Info("*** resharper-unity: Meta removed {0}", path);
                    }
                }
                catch (Exception e)
                {
                    myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.Assertion,
                        $"Failed to delete Unity meta file {path}");
                }
            }

            private void DoUnderTransaction(string command, Action action)
            {
                // Create a transaction - Rider will hook the file system and cause the VFS to refresh
                using (WriteLockCookie.Create())
                using (myMetaFileTracker.mySolution.CreateTransactionCookie(DefaultAction.Commit, command, NullProgressIndicator.Create()))
                    action();
            }
        }
    }
}