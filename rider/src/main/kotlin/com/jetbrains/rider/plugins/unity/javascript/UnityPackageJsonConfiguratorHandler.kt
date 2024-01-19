package com.jetbrains.rider.plugins.unity.javascript

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.ThreeState
import com.jetbrains.rider.plugins.appender.javascript.nodejs.RiderPackageJsonConfiguratorHandler
import com.jetbrains.rider.plugins.unity.getCompletedOr
import com.jetbrains.rider.plugins.unity.isUnityProject

class UnityPackageJsonConfiguratorHandler(private val project: Project) : RiderPackageJsonConfiguratorHandler {
    override fun isNpmPackageJson(packageJson: VirtualFile): ThreeState {
        return if (project.isUnityProject.getCompletedOr(false)) ThreeState.NO else ThreeState.UNSURE
    }
}