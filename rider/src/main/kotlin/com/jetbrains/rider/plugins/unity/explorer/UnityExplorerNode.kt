package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.PresentationData
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.SimpleTextAttributes
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.projectView.fileSystem.FileSystemNodeBase
import com.jetbrains.rider.projectView.nodes.calculateFileSystemIcon
import com.jetbrains.rider.projectView.nodes.containingProject
import com.jetbrains.rider.projectView.solutionName

open class UnityExplorerNode(project: Project, virtualFile: VirtualFile) : FileSystemNodeBase(project, virtualFile) {

    val nodes = ProjectModelViewHost.getInstance(project).getItemsByVirtualFile(virtualFile)

    public override fun hasProblemFileBeneath() = nodes.any { it.hasErrors() }

    override fun update(presentation: PresentationData?) {
        if (!virtualFile.isValid) return
        presentation?.addText(name, SimpleTextAttributes.REGULAR_ATTRIBUTES)
        presentation?.setIcon(virtualFile.calculateFileSystemIcon(project!!))

        // Add additional info for directories
        if (!virtualFile.isDirectory) return
        val projectNames = nodes
            .mapNotNull { it.containingProject() }
            .map { it.name.removePrefix(UnityExplorer.DefaultProjectPrefix + "-").removePrefix(UnityExplorer.DefaultProjectPrefix) }
            .filter { it.isNotEmpty() }
        if (projectNames.any()) {
            val description = projectNames.joinToString(", ")
            presentation?.addText(" ($description)", SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES)
        }
    }

    override fun createNode(virtualFile: VirtualFile): FileSystemNodeBase {
        return UnityExplorerNode(project!!, virtualFile)
    }

    override fun filterNode(node: FileSystemNodeBase): Boolean {
        // See https://docs.unity3d.com/Manual/SpecialFolders.html
        val file = node.virtualFile

        val extension = file.extension?.toLowerCase()
        if (extension != null && UnityExplorer.IgnoredExtensions.contains(extension.toLowerCase())) {
            return false
        }

        val name = file.nameWithoutExtension.toLowerCase()
        if (name == "cvs" || file.name.startsWith(".") || file.nameWithoutExtension.endsWith("~")) {
            return false
        }

        return true
    }

    class Root(project: Project, virtualFile: VirtualFile) : UnityExplorerNode(project, virtualFile) {

        override fun update(presentation: PresentationData?) {
            if (!virtualFile.isValid) return
            presentation?.presentableText = project!!.solutionName
            presentation?.setIcon(UnityExplorer.Icon)
        }

        override fun isAlwaysExpand() = true
    }
}