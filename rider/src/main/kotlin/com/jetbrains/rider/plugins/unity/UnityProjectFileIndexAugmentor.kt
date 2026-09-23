package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.project.Project
import com.intellij.openapi.roots.ProjectFileIndex
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.newvfs.NewVirtualFile
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ideaInterop.ProjectFileIndexAugmentor
import org.jetbrains.annotations.VisibleForTesting

class UnityProjectFileIndexAugmentor : ProjectFileIndexAugmentor {
  /**
   * Memoized [Project.projectDir]: resolving it stats the filesystem and walks the VFS, and this EP is called for
   * every file the platform asks about, so the uncached resolve dominated Search Everywhere traversals (RIDER-141491).
   * The EP is `area="IDEA_PROJECT"`, so one instance per project.
   *
   * [VirtualFile.isValid] is the only invalidation check needed — the solution directory is fixed for the session, and
   * a rename keeps the instance valid. It is also the only *safe* probe: after a VFS reconnect the cached file is
   * alien, and comparing it against a freshly resolved one would answer `false` where `isValid` says plainly that the
   * cache is stale.
   *
   * Held cache-avoiding; see [projectDir].
   *
   * Not `private` only so the test can plant a stale value.
   */
  @VisibleForTesting
  @Volatile
  var cachedProjectDir: VirtualFile? = null

  override fun isInProject(project: Project, index: ProjectFileIndex, file: VirtualFile, current: Boolean): Boolean {
    if (current) return true
    if (!project.isUnityProject.value) return false
    if (unityContentRootOf(project, file) == null) return false
    return !index.isExcluded(file)
  }

  override fun getContentRootForFile(
    project: Project,
    index: ProjectFileIndex,
    file: VirtualFile,
    honorExclusion: Boolean,
    current: VirtualFile?
  ): VirtualFile? {
    if (current != null) return current
    if (!project.isUnityProject.value) return null
    val root = unityContentRootOf(project, file) ?: return null
    if (honorExclusion && index.isExcluded(file)) return null
    return root
  }

  /**
   * Prunes Unity's generated directories — `Library/`, `Temp/`, `Logs/`, … — from `iterateContent`. Rider registers the
   * solution folder as one recursive `CONTENT_NON_INDEXABLE` set (IJPL-186432), so without this the walk also yields
   * Unity's caches: ~50,000 files under `Library/` alone on open-brush, which no consumer of `iterateContent` wants.
   *
   * **Names Unity's own output and nothing else.** The bound used to work the other way round — prune every directory
   * beside `Assets/` and `Packages/`, then let anything registered as a content root in its own right back through —
   * which made this code the arbiter of directories that are not Unity's. `.github/` and `docs/` survived only because
   * the Files view happens to register them, so a feature that never mentions Unity decided what a Unity solution
   * enumerates, and a recursive content root added later would have been quietly exempted by the same accident.
   * Naming only what Unity generates leaves every other directory to whoever owns it.
   *
   * The cost of that is honest: a directory a future Unity release invents is enumerated until it is added here. That
   * is the trade — an unlisted cache is noise in someone's file completion, while a wrongly pruned directory is
   * content that has silently disappeared, and only the second is invisible to the person it happens to.
   *
   * Two properties this must keep: **directories only**, so the `.sln` and the generated `.csproj`s are still
   * enumerated; and **content status does not move** — this bounds enumeration alone, whereas a workspace-model
   * exclusion was tried and reverted for costing packages their content status.
   */
  override fun skipFromIteration(project: Project, index: ProjectFileIndex, fileOrDir: VirtualFile): Boolean {
    // Ordered by cost: most visited nodes are files, and projectDir is resolved only for Unity solutions. The disposal
    // check comes before the first project service, because the non-recursive half of the walk has no scope guard of
    // its own and a solution can be closed while an iterateContent is in flight.
    if (!fileOrDir.isDirectory) return false
    if (project.isDisposed) return false
    if (!project.isUnityProject.value) return false
    val baseDir = projectDir(project)
    return isChildOf(baseDir, fileOrDir) && isUnityGeneratedDirectory(baseDir, fileOrDir)
  }

  override fun isInContent(
    project: Project,
    index: ProjectFileIndex,
    fileSet: com.intellij.workspaceModel.core.fileIndex.WorkspaceFileSetWithCustomData<*>,
    current: Boolean
  ): Boolean {
    if (current) return true
    // Fall back to isInProject augmentation: treat Unity roots as project/content
    return isInProject(project, index, fileSet.root, false)
  }

  /**
   * Publication-only memoization: a volatile read plus [VirtualFile.isValid] on the hit path, and **no lock** on the
   * miss path. Callers arrive under a shared read lock ([ProjectFileIndex] is `@RequiresReadLock`), so several threads
   * can miss at once and each resolves independently.
   *
   * That redundancy is deliberate. Deduplicating the resolve costs a monitor, and a read-lock holder parked on a
   * monitor can neither release the lock nor see `ProgressManager.checkCanceled()` — the very stall class this
   * memoization exists to remove, reintroduced on a path `master` never had one on. The duplicate work it would save
   * is one `File.isDirectory` plus one VFS lookup per VFS epoch, and [Project.projectDir] resolves through
   * `findFileByIoFile`, which returns canonical instances — so racing threads publish the *same* reference and the
   * relaxation cannot produce a torn or disagreeing cache.
   */
  private fun projectDir(project: Project): VirtualFile {
    cachedProjectDir?.let { if (it.isValid) return it }
    // Cache-avoiding because this file is held for the session and read on a traversal path: nothing here should be
    // able to populate a VFS cache on its behalf. Nothing it is asked today would -- the wrapper only diverts children
    // access and user data, and this file is only asked isValid, isCaseSensitive and its path -- but that is a fact
    // about the current callers, not a property of the cached value, and it is the value that outlives them.
    //
    // Guarded rather than calling NewVirtualFile.asCacheAvoiding directly, which throws IllegalArgumentException for
    // anything that is neither a NewVirtualFile nor already cache-avoiding. A solution directory is one in every shape
    // shipped today, but this augmentor used to accept any VirtualFile and a cache hint is not worth narrowing that to.
    val dir = project.projectDir
    val cacheAvoiding = if (dir is NewVirtualFile) dir.asCacheAvoiding() else dir
    return cacheAvoiding.also { cachedProjectDir = it }
  }

  /**
   * The `Assets`/`Packages` directory [file] lives in, or `null` if it is not under one.
   *
   * Walks up once instead of building the root list and testing ancestry against each entry: both callers run for every
   * file the platform asks about, which is the traffic RIDER-141491 was reported on.
   */
  private fun unityContentRootOf(project: Project, file: VirtualFile): VirtualFile? {
    val baseDir = projectDir(project)
    var candidate: VirtualFile? = file
    while (candidate != null) {
      if (isChildOf(baseDir, candidate)) {
        return if (candidate.isDirectory && isUnityContentRoot(baseDir, candidate)) candidate else null
      }
      candidate = candidate.parent
    }
    return null
  }

  /**
   * Whether [file] sits directly inside [baseDir], compared by path rather than by [VirtualFile] identity.
   *
   * `VirtualFile` equality cannot answer this under the cache-avoiding walk `iterateContent` runs. A
   * `TransientVirtualFileImpl` -- what `CacheAvoidingVirtualFileWrapper.getChildren` hands out for every uncached
   * child -- is not a `VirtualFileWithId`, and its `equals` opens with a `getClass()` check, so it is unequal to a
   * plain file in *either* direction. It happens to work today only because a transient's parent is the wrapper, whose
   * `equals` does compare VFS ids; the moment a transient's parent is itself transient, identity silently answers
   * `false` and the bound stops pruning. Paths are the one identity every shape here agrees on.
   *
   * Compared under the same case rule as [isUnityContentRoot] and [isUnityGeneratedDirectory]. A case-sensitive
   * compare here would have been a second, contradictory rule in one file: on a case-insensitive volume the same
   * directory can be spelled either way, and the bound would then quietly stop pruning for a solution opened by a
   * path whose case does not match the walk's.
   */
  private fun isChildOf(baseDir: VirtualFile, file: VirtualFile): Boolean {
    val parentPath = file.parent?.path ?: return false
    return parentPath.equals(baseDir.path, ignoreCase = !baseDir.isCaseSensitive)
  }

  /**
   * A name comparison, and deliberately not `dir == baseDir.findChild(name)`.
   *
   * Both callers establish [isChildOf] before asking, so the name is all that is left to decide; the `findChild`
   * round-trip re-derived a fact the caller already had, and paid a VFS lookup per visited node for it.
   *
   * It was also **wrong**. `iterateContent` walks the solution folder wrapped as cache-avoiding, and
   * `CacheAvoidingVirtualFileWrapper.getChildren` hands out a `TransientVirtualFileImpl` for every child not already
   * cached — whose `equals` starts `getClass() != o.getClass()`, so it is never equal to the plain `VirtualFile` that
   * `findChild` returns. `Assets/` arriving cold therefore failed this test and was pruned as junk, silently
   * reinstating RIDER-141737. Comparing names has no identity in it to break.
   *
   * [VirtualFile.isCaseSensitive] is per-directory, so the answer still follows the volume's own case rules: an
   * `assets/` directory is the project's real content root on a case-insensitive volume and must not be pruned.
   */
  private fun isUnityContentRoot(baseDir: VirtualFile, dir: VirtualFile): Boolean =
    unityContentRootNames.any { name -> dir.name.equals(name, ignoreCase = !baseDir.isCaseSensitive) }

  /** Compared the same way as [isUnityContentRoot], so one case rule governs the whole file. */
  private fun isUnityGeneratedDirectory(baseDir: VirtualFile, dir: VirtualFile): Boolean =
    unityGeneratedDirNames.any { name -> dir.name.equals(name, ignoreCase = !baseDir.isCaseSensitive) }
}

/** The directories in a Unity solution folder that hold the project's own files. */
private val unityContentRootNames = listOf("Assets", "Packages")

/**
 * The directories Unity generates in the solution folder, and the only ones this bound prunes.
 *
 * Taken from the root-anchored directory entries of the canonical `Unity.gitignore`, which is the closest thing to
 * an authoritative statement of what a Unity project regenerates; [UnityIgnoredFileProvider] keeps VCS out of the
 * same set, and the two agree except that it does not list the newer `Recordings/`, `ServerData/` and
 * `UIElementsSchema/`. The case variants both of them spell out (`[Ll]ibrary`) are dropped here because case is
 * decided per volume — see [UnityProjectFileIndexAugmentor.isUnityGeneratedDirectory].
 *
 * `Recordings/` earns its place on size: a project using Unity Recorder can fill it with captures, and enumerating
 * those is the same performance cliff RIDER-141491 was filed about.
 *
 * Deliberately *not* `ProjectSettings/`: Unity writes it, but it is version-controlled project state alongside
 * `Assets/` and `Packages/`, so pruning it would be hiding the project's own files. `UserSettings/` **is** listed —
 * Unity writes it too, but it is per-user and disposable, which is why the gitignore has it and `ProjectSettings/`
 * is absent from it. (`UnityWorkspaceFileIndexContributor` registers both as `EXTERNAL_SOURCE`, which does *not*
 * keep either out of the walk — the solution-folder blanket still covers them.)
 */
private val unityGeneratedDirNames = listOf(
  "Library", "Temp", "Obj", "Build", "Builds", "Logs", "UserSettings", "MemoryCaptures", "Recordings",
  "ServerData", "UIElementsSchema", ".utmp",
)
