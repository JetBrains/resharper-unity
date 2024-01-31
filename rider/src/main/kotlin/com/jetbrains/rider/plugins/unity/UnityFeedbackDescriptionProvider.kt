@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity

import com.intellij.ide.FeedbackDescriptionProvider
import com.intellij.openapi.project.Project
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.workspace.tryGetPackage

class UnityFeedbackDescriptionProvider : FeedbackDescriptionProvider {
    override suspend fun getDescription(project: Project?): String? {
        if (project == null) return null
        val version = UnityInstallationFinder.getInstance(project).getApplicationVersion() ?: return null
        val packageVersion = WorkspaceModel.getInstance(project).tryGetPackage("com.unity.ide.rider")?.version ?: "missing"
        return "Unity: $version, JetBrains Rider package: $packageVersion"
    }
}