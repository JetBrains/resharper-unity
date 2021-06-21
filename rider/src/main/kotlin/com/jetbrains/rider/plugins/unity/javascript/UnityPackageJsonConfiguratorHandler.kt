package com.jetbrains.rider.plugins.unity.javascript

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.appender.javascript.nodejs.RiderPackageJsonConfiguratorHandler
import com.intellij.util.ThreeState

class UnityPackageJsonConfiguratorHandler(private val project: Project) : RiderPackageJsonConfiguratorHandler {
    override fun isNpmPackageJson(packageJson: VirtualFile): ThreeState {
        return if (project.isUnityProject()) ThreeState.NO else ThreeState.UNSURE
    }
}