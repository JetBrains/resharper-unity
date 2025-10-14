package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.roots.ContentIterator
import com.intellij.openapi.roots.ContentIteratorEx
import com.intellij.openapi.roots.ProjectFileIndex
import com.intellij.openapi.vfs.VfsUtilCore
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFileFilter
import com.intellij.openapi.vfs.VirtualFileVisitor
import com.intellij.util.containers.TreeNodeProcessingResult
import com.intellij.workspaceModel.core.fileIndex.impl.WorkspaceFileIndexEx
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.ideaInterop.ProjectFileIndexAugmentor

class UnityProjectFileIndexAugmentor : ProjectFileIndexAugmentor {
  override fun isInProject(project: Project, index: ProjectFileIndex, file: VirtualFile, current: Boolean): Boolean {
    if (current) return true
    if (!project.isUnityProject.value) return false
    if (index.isExcluded(file)) return false
    return unityRoots(project).any { root -> VfsUtilCore.isAncestor(root, file, false) }
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
    if (honorExclusion && index.isExcluded(file)) return null
    return unityRoots(project).firstOrNull { root -> VfsUtilCore.isAncestor(root, file, false) }
  }

  override fun iterateExtraContent(
    project: Project,
    index: ProjectFileIndex,
    processor: ContentIterator,
    filter: VirtualFileFilter?
  ): Boolean {
    if (!project.isUnityProject.value) return true

    val extraRoots = unityRoots(project).filter { it.isValid && it.isDirectory }
    if (extraRoots.isEmpty()) return true

    val processorEx = toContentIteratorEx(processor)
    val workspaceIndex = WorkspaceFileIndexEx.getInstance(project)
    for (root in extraRoots) {
      // If Unity root is already covered by a parent recursive content root in Workspace Model, skip to avoid duplicate traversal
      val parentContentRoot = ApplicationManager.getApplication().runReadAction<VirtualFile?> {
        workspaceIndex.getContentFileSetRoot(root, true)
      }
      if (parentContentRoot != null && parentContentRoot != root) continue

      if (!iterateUnityContentUnderDirectory(project, index, root, processorEx, filter)) return false
    }
    return true
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

  private fun iterateUnityContentUnderDirectory(
    project: Project,
    index: ProjectFileIndex,
    dir: VirtualFile,
    processor: ContentIteratorEx,
    customFilter: VirtualFileFilter?,
  ): Boolean {
    val workspaceIndex = WorkspaceFileIndexEx.getInstance(project)
    val visitor = object : VirtualFileVisitor<Void?>() {
      override fun visitFileEx(file: VirtualFile): Result {
        if (project.isDisposed) return skipTo(dir)

        // Apply user filter early: if a directory is filtered out, skip its children entirely
        if (customFilter != null && !customFilter.accept(file)) {
          return if (file.isDirectory) SKIP_CHILDREN else CONTINUE
        }

        // exclusions/ignored
        val excludedOrIgnored = ApplicationManager.getApplication().runReadAction<Boolean> {
          index.isExcluded(file) || index.isUnderIgnored(file)
        }
        if (excludedOrIgnored) {
          return if (file.isDirectory) SKIP_CHILDREN else CONTINUE
        }

        // If a directory is already under a recursive content root provided by Workspace Model, skip its subtree
        if (file.isDirectory) {
          val parentContentRoot = ApplicationManager.getApplication().runReadAction<VirtualFile?> {
            workspaceIndex.getContentFileSetRoot(file, true)
          }
          if (parentContentRoot != null && parentContentRoot != file) {
            return SKIP_CHILDREN
          }
        }

        // Avoid duplicates for files already in workspace content
        val alreadyInWorkspace = ApplicationManager.getApplication().runReadAction<Boolean> {
          workspaceIndex.getContentFileSetRoot(file, true) != null
        }
        if (!alreadyInWorkspace) {
          val status = processor.processFileEx(file)
          return when (status) {
            TreeNodeProcessingResult.CONTINUE -> CONTINUE
            TreeNodeProcessingResult.SKIP_CHILDREN -> SKIP_CHILDREN
            TreeNodeProcessingResult.SKIP_TO_PARENT -> skipTo(file.parent)
            TreeNodeProcessingResult.STOP -> skipTo(dir)
          }
        }

        return CONTINUE
      }
    }
    val result = VfsUtilCore.visitChildrenRecursively(dir, visitor)
    return result.skipToParent != dir
  }

  private fun toContentIteratorEx(processor: ContentIterator): ContentIteratorEx {
    return processor as? ContentIteratorEx
           ?: ContentIteratorEx { fileOrDir ->
             if (processor.processFile(fileOrDir)) TreeNodeProcessingResult.CONTINUE else TreeNodeProcessingResult.STOP
           }
  }

  private fun unityRoots(project: Project): List<VirtualFile> {
    val baseDir = project.projectDir
    val roots = mutableListOf<VirtualFile>()
    baseDir.findChild("Assets")?.let { if (it.isValid && it.isDirectory) roots.add(it) }
    baseDir.findChild("Packages")?.let { if (it.isValid && it.isDirectory) roots.add(it) }
    return roots
  }
}
