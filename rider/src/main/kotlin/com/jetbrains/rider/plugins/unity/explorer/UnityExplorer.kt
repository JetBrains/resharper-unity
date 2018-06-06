package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.ide.SelectInContext
import com.intellij.ide.projectView.ProjectView
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DataKey
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ToggleAction
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.JDOMExternalizerUtil
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.icons.ReSharperSolutionAnalysisIcons
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.nodes.IProjectModelNode
import com.jetbrains.rider.projectView.views.SolutionViewPaneBase
import com.jetbrains.rider.projectView.views.impl.SolutionViewSelectInTargetBase
import org.jdom.Element

class UnityExplorer(project: Project) : SolutionViewPaneBase(project, UnityExplorerRootNode(project)) {

    companion object {
        const val ID = "UnityExplorer"
        const val Title = "Unity"
        const val Weight = 1
        const val ShowHiddenItemsOption = "show-hidden-items"
        val Icon = UnityIcons.Toolwindows.ToolWindowUnityLog

        const val DefaultProjectPrefix = "Assembly-CSharp"
        val IgnoredExtensions = hashSetOf("meta", "tmp")

        val SELECTED_REFERENCE_KEY: DataKey<UnityExplorerNode.ReferenceItem> = DataKey.create("selectedReference")

        fun getInstance(project: Project): UnityExplorer {
            return tryGetInstance(project)!!
        }
        fun tryGetInstance(project: Project): UnityExplorer? {
            return ProjectView.getInstance(project).getProjectViewPaneById(ID) as? UnityExplorer
        }
    }

    var myShowHiddenItems = false

    override fun isInitiallyVisible(): Boolean {
        return UnityReferenceDiscoverer.hasAssetsFolder(project)
    }

    override fun getData(dataId: String?): Any? {
        return when {
            SELECTED_REFERENCE_KEY.`is`(dataId) -> getSelectedNodes().filterIsInstance<UnityExplorerNode.ReferenceItem>().singleOrNull()
            else -> super.getData(dataId)
        }
    }

    override fun getSelectedProjectModelNodes(): Array<IProjectModelNode> {
        return getSelectedNodes().filterIsInstance<UnityExplorerNode>().flatMap { it.nodes.asIterable() }.toTypedArray()
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
    override fun getWeight() = Weight

    override fun createSelectInTarget() =  object : SolutionViewSelectInTargetBase(project) {
        override fun selectIn(context: SelectInContext?, requestFocus: Boolean) {
            context?.let { select(it, null, requestFocus) }
        }
        override fun toString() = Title
        override fun getMinorViewId() = ID
        override fun getWeight() = Weight.toFloat()
    }

    override fun addPrimaryToolbarActions(actionGroup: DefaultActionGroup) {
        actionGroup.addAction(ShowHiddenItemsAction())
        actionGroup.addSeparator()
        super.addPrimaryToolbarActions(actionGroup)
    }

//    override fun getStripe(data: Any?, expanded: Boolean): ErrorStripe? {
//        if (expanded) {
//            val node = data as? UnityExplorerNode
//            if (node != null && !node.hasProblemFileBeneath()) {
//                return null
//            }
//        }
//        return super.getStripe(data, expanded)
//    }

    private inner class ShowHiddenItemsAction
        : ToggleAction("Show Hidden Files", null, ReSharperSolutionAnalysisIcons.UnignoreErrors), DumbAware {

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