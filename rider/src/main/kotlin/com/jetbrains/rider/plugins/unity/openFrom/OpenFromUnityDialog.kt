package com.jetbrains.rider.plugins.unity.openFrom

import com.intellij.openapi.ui.DialogWrapper
import com.intellij.openapi.ui.panel.ComponentPanelBuilder
import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.ScrollPaneFactory
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.components.JBList
import com.jetbrains.rider.plugins.unity.restClient.ProjectState
import java.awt.Component
import java.awt.Dimension
import java.nio.file.Paths
import javax.swing.*

class OpenFromUnityDialog(private val discoverer: UnityOpenProjectDiscoverer)
    : DialogWrapper(null) {

    val selectedUnityProject: OpenUnityProject?
        get() = list.selectedValue

    private val listModel = DefaultListModel<OpenUnityProject>()
    private val listModelLock = Object()
    private val list = JBList<OpenUnityProject>()
    private val rootPanel = JPanel()

    init {
        title = "Open project from Unity Editor"
        list.setPaintBusy(true)
        list.emptyText.text = "Finding open Unity Editor projects…"
        list.model = listModel
        list.cellRenderer = OpenUnityProjectCellRenderer()
        setOKButtonText("Open project")
        isOKActionEnabled = false

        list.selectionMode = ListSelectionModel.SINGLE_SELECTION
        list.selectionModel.addListSelectionListener {
            isOKActionEnabled = list.selectedIndex != -1
        }

        init()
    }

    override fun createCenterPanel(): JComponent? {
        rootPanel.layout = BoxLayout(rootPanel, BoxLayout.PAGE_AXIS)
        val pane = ScrollPaneFactory.createScrollPane(list)
        pane.alignmentX = Component.LEFT_ALIGNMENT
        pane.preferredSize = Dimension(400, 100)
        rootPanel.add(pane)
        val comment = ComponentPanelBuilder.createCommentComponent("Please wait. This can take several seconds.", true)
        rootPanel.add(comment)
        return rootPanel
    }

    override fun show() {

        // TODO: Report errors
        discoverer.start({
            synchronized(listModelLock) {
                val wasEmpty = listModel.isEmpty
                listModel.addElement(it)
                if (wasEmpty)
                    list.selectedIndex = 0
            }
        }, {
            list.setPaintBusy(false)
            synchronized(listModelLock) {
                if (listModel.isEmpty) {
                    list.emptyText.text = "No open projects found"
                }
            }
        })

        super.show()
    }

    class OpenUnityProjectCellRenderer : ColoredListCellRenderer<OpenUnityProject>() {
        override fun customizeCellRenderer(list: JList<out OpenUnityProject>, openProject: OpenUnityProject?, index: Int, selected: Boolean, hasFocus: Boolean) {
            openProject ?: return

            val baseDirectory = openProject.projectState.basedirectory
            val projectName = openProject.projectName
            append(projectName, SimpleTextAttributes.REGULAR_ATTRIBUTES)
            append(" ($baseDirectory)", SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES)
        }
    }
}

class OpenUnityProject(val port: Int, val projectState: ProjectState) {

    private val projectPath = Paths.get(projectState.basedirectory)

    val projectName = projectPath.fileName.toString()
    val solutionFile = projectPath.resolve(projectName + ".sln").toFile()
}

