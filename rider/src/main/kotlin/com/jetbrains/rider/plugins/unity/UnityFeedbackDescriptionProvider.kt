package com.jetbrains.rider.plugins.unity

import com.intellij.ide.FeedbackDescriptionProvider
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder

class UnityFeedbackDescriptionProvider: FeedbackDescriptionProvider {
    override fun getDescription(project: Project?): String? {
        if (project == null) return null
        val version = UnityInstallationFinder.getInstance(project).getApplicationVersion() ?: return null
        return "Unity: $version"
    }
}