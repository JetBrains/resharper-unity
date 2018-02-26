package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.ProjectModelViewProviderBase

class UnityExplorerViewProvider(project: Project) : ProjectModelViewProviderBase(project) {
    override fun getProjectViewPaneId() = UnityExplorer.ID
}