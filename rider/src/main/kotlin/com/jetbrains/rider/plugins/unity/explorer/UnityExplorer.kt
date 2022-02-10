package com.jetbrains.rider.plugins.unity.explorer

import com.intellij.icons.AllIcons
import com.intellij.ide.SelectInContext
import com.intellij.ide.projectView.ProjectView
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.JDOMExternalizerUtil
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectView.views.SolutionViewPaneBase
import com.jetbrains.rider.projectView.views.actions.ConfigureScratchesAction
import com.jetbrains.rider.projectView.views.actions.SolutionViewToggleAction
import com.jetbrains.rider.projectView.views.impl.SolutionViewSelectInTargetBase
import com.jetbrains.rider.projectView.views.solutionExplorer.SolutionExplorerViewPane
import icons.UnityIcons
import org.jdom.Element

class UnityExplorer(project: Project) : SolutionViewPaneBase(project, createRootNode(project)) {

    companion object {
        const val ID = "UnityExplorer"
        const val Title = "Unity"
        const val Weight = 1
        const val ShowProjectNamesOption = "show-project-names"
        const val ShowTildeFoldersOption = "show-tilde-folders"
        const val DefaultProjectPrefix = "Assembly-CSharp"

        val DefaultProjectPrefixRegex = ("$DefaultProjectPrefix[.-]?").toRegex()

        val Icon = UnityIcons.ToolWindows.UnityExplorer
        val IgnoredExtensions = hashSetOf("meta", "tmp")

        fun getInstance(project: Project) = tryGetInstance(project)!!

        fun tryGetInstance(project: Project): UnityExplorer? {
            return ProjectView.getInstance(project).getProjectViewPaneById(ID) as? UnityExplorer
        }

        private fun createRootNode(project: Project): UnityExplorerRootNode {
            return UnityExplorerRootNode(project)
        }
    }

    var showTildeFolders = true
        private set

    var showProjectNames = true
        private set

    fun hasPackagesRoot(): Boolean {
        // The tree's model is cached, while this.model.root.children isn't. This is important in that it reflects the
        // current state of the model before it's had a chance to be invalidated. Note that `tree` isn't valid until the
        // component has been created, so it will be null if the pane is hidden
        if (this.tree == null) {
            return false
        }
        val root = tree.model.root
        val count = tree.model.getChildCount(root)
        for (i in 0..count) {
            if (tree.model.getChild(root, i) is PackagesRootNode) {
                return true
            }
        }
        return false
    }

    override fun isInitiallyVisible() = project.isUnityProject()

    override fun writeExternal(element: Element) {
        super.writeExternal(element)
        JDOMExternalizerUtil.writeField(element, ShowProjectNamesOption, showProjectNames.toString())
        JDOMExternalizerUtil.writeField(element, ShowTildeFoldersOption, showTildeFolders.toString())
    }

    override fun readExternal(element: Element) {
        super.readExternal(element)
        var option = JDOMExternalizerUtil.readField(element, ShowProjectNamesOption)
        showProjectNames = option == null || java.lang.Boolean.parseBoolean(option)
        option = JDOMExternalizerUtil.readField(element, ShowTildeFoldersOption)
        showTildeFolders = option == null || java.lang.Boolean.parseBoolean(option)
    }

    override fun getTitle() = Title
    override fun getIcon() = Icon
    override fun getId() = ID
    override fun getWeight() = Weight

    override fun createSelectInTarget() =  object : SolutionViewSelectInTargetBase(project) {

        // We have to return true here, because a file might be from a local package, which could be almost anywhere on
        // the filesystem
        override fun canSelect(context: SelectInContext) = true

        override fun selectIn(context: SelectInContext?, requestFocus: Boolean) {
            context?.let { select(it, null, requestFocus) }
        }

        override fun toString() = Title
        override fun getMinorViewId() = ID
        override fun getWeight() = Weight.toFloat()
    }

    // Adds to the tool window toolbar
    override fun addToolbarActions(actionGroup: DefaultActionGroup) {
        actionGroup.addAction(ConfigureScratchesAction()).setAsSecondary(true)
        actionGroup.addAction(SolutionViewToggleAction("Show Project Names",
            "Show names of owning projects next to folders",
            AllIcons.Actions.ListFiles,
            { showProjectNames }, { showProjectNames = it }
        )).setAsSecondary(true)
        actionGroup.addAction(SolutionViewToggleAction("Show Hidden Folders",
            "Show folders ending with '~'",
            AllIcons.Actions.ListFiles,
            { showTildeFolders }, { showTildeFolders = it }
        )).setAsSecondary(true)
        super.addToolbarActions(actionGroup)
    }

    // Adds to the project view pane's own toolbar
    override fun addPrimaryToolbarActions(actionGroup: DefaultActionGroup) {
        actionGroup.addAction(SolutionViewToggleAction("Show All Files",
            "Show all files, including .meta files",
            AllIcons.Actions.ShowHiddens, {
                SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles
            }, {
                SolutionExplorerViewPane.getInstance(myProject).myShowAllFiles = it
        }))
        ActionManager.getInstance().getAction("ViewInAssemblyExplorer")!!.let { actionGroup.addAction(it) }
        actionGroup.addSeparator()
        super.addPrimaryToolbarActions(actionGroup)
    }
}
