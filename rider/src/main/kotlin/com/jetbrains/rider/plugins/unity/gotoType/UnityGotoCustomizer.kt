package com.jetbrains.rider.plugins.unity.gotoType

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.gotoType.GotoCustomizer
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml.UnityYamlFileType
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectView.solutionDirectory

class UnityGotoCustomizer : GotoCustomizer {
    // RIDER-117479
    override fun isGotoTargetInProject(project: Project, file: VirtualFile): Boolean {
        if (!project.isUnityProject.value) return false
        if (file.fileType !is UnityYamlFileType) return false
        val solutionDir = project.solutionDirectory.toVirtualFile(false)?:return false
        val assetsFolder = solutionDir.findChild("Assets") ?: return false
        if (VfsUtil.isAncestor(assetsFolder, file, false)) return true

        val localPackagesFolder = solutionDir.findChild("Packages") ?: return false
        return VfsUtil.isAncestor(localPackagesFolder, file, false)
    }
}