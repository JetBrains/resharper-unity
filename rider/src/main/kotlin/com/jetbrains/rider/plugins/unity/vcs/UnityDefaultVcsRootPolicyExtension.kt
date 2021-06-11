package com.jetbrains.rider.plugins.unity.vcs

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.io.isAncestor
import com.jetbrains.rider.ideaInterop.DefaultVcsRootPolicyExtension
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder

class UnityDefaultVcsRootPolicyExtension(project: Project) : DefaultVcsRootPolicyExtension(project) {

    override fun filter(root: VirtualFile): Boolean {
        val packagesRoot = UnityInstallationFinder.getInstance(project).getBuiltInPackagesRoot() ?: return false
        if (packagesRoot.isAncestor(root.toNioPath())) {
            return true
        }
        return false
    }
}