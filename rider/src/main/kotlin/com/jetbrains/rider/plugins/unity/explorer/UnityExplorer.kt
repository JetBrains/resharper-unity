package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.projectView.ProjectView
import com.intellij.ide.util.treeView.AbstractTreeNode
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DataKey
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ToggleAction
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.JDOMExternalizerUtil
import com.intellij.ui.stripe.ErrorStripe
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.ProjectModelDataKeys
import com.jetbrains.rider.projectView.fileSystem.FileSystemNodeBase
import com.jetbrains.rider.projectView.fileSystem.FileSystemViewPaneBase
import com.jetbrains.rider.projectView.nodes.IProjectModelNode
import com.jetbrains.rider.projectView.nodes.VirtualProjectModelNode
import org.jdom.Element

class UnityExplorer(project: Project) : FileSystemViewPaneBase(project) {

    companion object {
        const val ID = "UnityExplorer"
        const val Title = "Unity Explorer"
        const val ShowHiddenItemsOption = "show-hidden-items"
        val Icon = UnityIcons.Toolwindows.ToolWindowUnityLog

        const val DefaultProjectPrefix = "Assembly-CSharp"
        val IgnoredExtensions = hashSetOf("meta", "tmp")

        val SELECTED_REFERENCE_KEY: DataKey<UnityExplorerNode.ReferenceItem> = DataKey.create("selectedReference")
    }

    var myShowHiddenItems = false

    override fun isInitiallyVisible(): Boolean {
        return UnityReferenceDiscoverer.hasAssetsFolder(project)
    }

    override fun createRootNode(): FileSystemNodeBase {
        val assetsFolder = project.baseDir?.findChild("Assets")!!
        return UnityExplorerNode.Root(project, assetsFolder, this)
    }

    override fun getData(selected: MutableList<AbstractTreeNode<Any>>, dataId: String?): Any? {
        return when {
            ProjectModelDataKeys.PROJECT_MODEL_NODES.`is`(dataId) -> getProjectModelNodes(selected).toTypedArray()
            SELECTED_REFERENCE_KEY.`is`(dataId) -> selected.filterIsInstance<UnityExplorerNode.ReferenceItem>().singleOrNull()
            else -> null
        }
    }

    private fun getProjectModelNodes(selected: MutableList<AbstractTreeNode<Any>>): List<IProjectModelNode> {
        return selected.filterIsInstance<UnityExplorerNode>().map {
            if (it.nodes.any()) return@map it.nodes.filter { it.isValid() }
            return arrayListOf<IProjectModelNode>(VirtualProjectModelNode(it.project!!, it.virtualFile, null))
        }.flatMap { it.asIterable() }
    }

    override fun writeExternal(element: Element) {
        super.writeExternal(element)
        JDOMExternalizerUtil.writeField(element, ShowHiddenItemsOption, myShowHiddenItems.toString())
    }

    override fun readExternal(element: Element) {
        super.readExternal(element)
        val option = JDOMExternalizerUtil.readField(element, ShowHiddenItemsOption)
        myShowHiddenItems = option != null && java.lang.Boolean.parseBoolean(option)
    }

    override fun getTitle() = Title
    override fun getIcon() = Icon
    override fun getId() = ID
    override fun getWeight() = -1
    override fun createSelectInTarget() = UnityExplorerSelectInTarget(project)

    override fun supportsSortByType() = false
    override fun supportsFoldersAlwaysOnTop() = false

    override fun addToolbarActions(actionGroup: DefaultActionGroup?) {
        actionGroup?.addAction(ShowHiddenItemsAction())?.setAsSecondary(true)
    }

    override fun getStripe(data: Any?, expanded: Boolean): ErrorStripe? {
        if (expanded) {
            val node = data as? UnityExplorerNode
            if (node != null && !node.hasProblemFileBeneath()) {
                return null
            }
        }
        return super.getStripe(data, expanded)
    }

    private inner class ShowHiddenItemsAction : ToggleAction("Show Hidden Items"), DumbAware {

        override fun isSelected(event: AnActionEvent): Boolean {
            return myShowHiddenItems
        }

        override fun setSelected(event: AnActionEvent, flag: Boolean) {
            if (myShowHiddenItems != flag) {
                myShowHiddenItems = flag
                updateFromRoot(false)
            }
        }

        override fun update(e: AnActionEvent) {
            super.update(e)
            e.presentation.isEnabledAndVisible = ProjectView.getInstance(myProject).currentProjectViewPane === this@UnityExplorer
        }
    }
}