#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    // Creates real project items for external files. The C++ engine does not analyse a file that has only a Misc Files
    // item. Only Rider implements this, because ReSharper must not add items that Visual Studio does not know.
    public interface IUnityExternalProjectFileCreator
    {
        // Matching files get a real project item instead of an external files module entry.
        bool RequiresProjectFile(IPath path);

        // Runs deferred and must be safe to repeat. onAdopted gets the paths that now have a real item.
        void CreateProjectFiles(IReadOnlyList<VirtualFileSystemPath> paths,
                                Action<IReadOnlyList<VirtualFileSystemPath>> onAdopted);

        // True only for an item this creator made.
        bool OwnsItem(IProjectItem item);
    }
}
