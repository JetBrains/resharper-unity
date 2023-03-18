package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.io.isAncestor
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.projectView.baseDirectories.ExternalDirectoryProvider

class UnityExternalDirectoryProvider(private val project: Project) : ExternalDirectoryProvider {
    override fun isInExternalDirectory(virtualFile: VirtualFile): Boolean {
        val packagesRoot = UnityInstallationFinder.getInstance(project).getBuiltInPackagesRoot() ?: return false
        return packagesRoot.isAncestor(virtualFile.toNioPath())
    }
}