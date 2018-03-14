package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.project.Project
import com.intellij.ui.stripe.ErrorStripe
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.ProjectModelDataKeys
import com.jetbrains.rider.projectView.fileSystem.FileSystemNodeBase
import com.jetbrains.rider.projectView.fileSystem.FileSystemViewPaneBase
import com.jetbrains.rider.projectView.nodes.IProjectModelNode
import com.jetbrains.rider.projectView.nodes.VirtualProjectModelNode

class UnityExplorer(project: Project) : FileSystemViewPaneBase(project) {

    companion object {
        const val ID = "UnityExplorer"
        const val Title = "Unity Explorer"
        val Icon = UnityIcons.Logo

        const val DefaultProjectPrefix = "Assembly-CSharp"
        val IgnoredExtensions = hashSetOf("meta", "tmp")
    }

    override fun isInitiallyVisible(): Boolean {
        val assetsFolder = project.baseDir?.findChild("Assets")
        return assetsFolder != null
    }

    override fun createRootNode(): FileSystemNodeBase {
        val assetsFolder = project.baseDir?.findChild("Assets")!!
        return UnityExplorerNode.Root(project, assetsFolder)
    }

    override fun getData(selected: MutableList<AbstractTreeNode<Any>>, dataId: String?): Any? {
        when {
            ProjectModelDataKeys.PROJECT_MODEL_NODES.`is`(dataId) -> return getProjectModelNodes(selected).toTypedArray()
        }
        return null
    }

    private fun getProjectModelNodes(selected: MutableList<AbstractTreeNode<Any>>): List<IProjectModelNode> {
        return selected.filterIsInstance<UnityExplorerNode>().map {
            if (it.nodes.any()) return@map it.nodes.filter { it.isValid() }
            return arrayListOf<IProjectModelNode>(VirtualProjectModelNode(it.project!!, it.virtualFile, null))
        }.flatMap { it.asIterable() }
    }

    override fun getTitle() = Title
    override fun getIcon() = Icon
    override fun getId() = ID
    override fun getWeight() = -1
    override fun createSelectInTarget() = UnityExplorerSelectInTarget(project)

    override fun supportsSortByType() = false
    override fun supportsFoldersAlwaysOnTop() = false

    override fun getStripe(data: Any?, expanded: Boolean): ErrorStripe? {
        if (expanded) {
            val node = data as? UnityExplorerNode
            if (node != null && !node.hasProblemFileBeneath()) {
                return null
            }
        }
        return super.getStripe(data, expanded)
    }
}