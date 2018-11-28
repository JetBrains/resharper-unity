package com.jetbrains.rider.plugins.unity.javascript

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.isLikeUnityProject
import com.jetbrains.rider.plugins.javascript.nodejs.RiderPackageJsonConfiguratorHandler
import com.intellij.util.ThreeState

class UnityPackageJsonConfiguratorHandler(private val project: Project) : RiderPackageJsonConfiguratorHandler {
    override fun isNpmPackageJson(packageJson: VirtualFile): ThreeState {
        return if (project.isLikeUnityProject()) ThreeState.NO else ThreeState.UNSURE
    }
}