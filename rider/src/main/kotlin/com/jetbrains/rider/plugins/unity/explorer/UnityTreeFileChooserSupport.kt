package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.ViewSettings
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.jetbrains.rider.ideaInterop.RiderTreeFileChooserSupport
import com.jetbrains.rider.plugins.unity.isUnityProject
import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
class UnityTreeFileChooserSupport(project: Project) : RiderTreeFileChooserSupport(project) {
    override fun createRoot(settings: ViewSettings): AbstractTreeNode<*> {
        if(project.isUnityProject.value)
            return UnityExplorerRootNode(project)
        return super.createRoot(settings)
    }
}