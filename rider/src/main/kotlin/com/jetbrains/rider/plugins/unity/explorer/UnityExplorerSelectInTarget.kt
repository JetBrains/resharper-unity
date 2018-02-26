package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.impl.ProjectPaneSelectInTarget
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project

class UnityExplorerSelectInTarget(val project: Project) : ProjectPaneSelectInTarget(project), DumbAware {
    override fun toString() = UnityExplorer.Title
    override fun getMinorViewId() = UnityExplorer.ID
    override fun getWeight() = 0f
}