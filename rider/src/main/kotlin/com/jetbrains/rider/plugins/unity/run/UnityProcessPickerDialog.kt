package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.progress.Task
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.defineNestedLifetime
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.openapi.ui.ValidationInfo
import com.intellij.openapi.ui.VerticalFlowLayout
import com.intellij.ui.*
import com.intellij.ui.components.JBList
import com.intellij.ui.components.dialog
import java.awt.BorderLayout
import java.awt.Dimension
import java.awt.FlowLayout
import java.awt.Insets
import java.awt.event.MouseEvent
import javax.swing.*

class UnityProcessPickerDialog(private val project: Project) : DialogWrapper(project) {

    private val listModel = DefaultListModel<UnityPlayer>()
    private val listModelLock = Object()
    private val list = JBList<UnityPlayer>()
    private val peerPanel: JPanel = JPanel()

    init {
        title = "Searching for Unity Editors and Players..."
        list.model = listModel
        list.cellRenderer = UnityProcessCellRenderer()
        isOKActionEnabled = false

        list.selectionMode = ListSelectionModel.SINGLE_SELECTION
        list.selectionModel.addListSelectionListener {
            isOKActionEnabled = list.selectedIndex != -1
            if (list.selectedIndex != -1) {
                isOKActionEnabled = list.selectedValue.allowDebugging
            }
        }

        object: DoubleClickListener() {
            override fun onDoubleClick(p0: MouseEvent?): Boolean {
                if (list.selectedIndex != -1) {
                    doOKAction()
                }
                return true
            }
        }.installOn(list)

        cancelAction.putValue(FOCUSED_ACTION, true)
        init()
        setResizable(false)
    }

    // DialogWrapper only lets the Mac set the preferred component via FOCUSED_ACTION because reasons
    override fun getPreferredFocusedComponent(): JComponent? {
        return myPreferredFocusedComponent
    }

    override fun createCenterPanel(): JComponent? {
        peerPanel.layout = BoxLayout(peerPanel, BoxLayout.PAGE_AXIS)
        val pane = ScrollPaneFactory.createScrollPane(list)
        pane.preferredSize = Dimension(600, 200)
        val customProcessButton = JButton()
        customProcessButton.text = "Enter address of remote process"
        customProcessButton.addActionListener {
            enterCustomIp()
        }
        val toolsPanel = JPanel(FlowLayout(FlowLayout.LEFT))
        toolsPanel.add(customProcessButton)
        peerPanel.add(pane, BorderLayout.NORTH)
        peerPanel.add(toolsPanel, BorderLayout.SOUTH)
        return peerPanel
    }

    override fun createHelpButton(insets: Insets): JButton {
        val button = super.createHelpButton(insets)
        button.isVisible = false
        return button
    }

    override fun doOKAction() {
        if (okAction.isEnabled) {
            val player = list.selectedValue
            if (player != null && player.allowDebugging) {
                UnityRunUtil.attachToUnityProcess(player.host, player.debuggerPort, player.id, project, player.isEditor)
            }
            close(OK_EXIT_CODE)
        }
    }

    override fun show() {
        object : Task.Backgroundable(project, "Getting list of Unity processes...") {
            override fun run(indicator: ProgressIndicator) {
                val lifetimeDefinition = project.defineNestedLifetime()
                try {
                    UnityPlayerListener({
                        synchronized(listModelLock) {
                            listModel.addElement(it)
                        }
                    }, {
                        synchronized(listModelLock) {
                            listModel.removeElement(it)
                        }
                    }, lifetimeDefinition.lifetime)
                } finally {
                    lifetimeDefinition.terminate()
                }
            }
        }.queue()
        super.show()
    }

    private fun enterCustomIp() {
        val hostField = JTextField("127.0.0.1")
        val portField = PortField(0)

        val panel = JPanel()
        panel.layout = VerticalFlowLayout()
        panel.add(hostField)
        panel.add(portField)

        val dialog = dialog(
                title = "Enter address of remote process",
                panel = panel,
                focusedComponent = hostField,
                project = project,
                parent = peerPanel) {
            val hostAddress = hostField.text
            portField.commitEdit()
            val port = portField.number
            val validationResult = mutableListOf<ValidationInfo>()

            if (hostAddress.isNullOrBlank()) validationResult.add(ValidationInfo("Host address must not be empty."))
            if (port <= 0) validationResult.add(ValidationInfo("Port number must be positive."))

            if (validationResult.count() > 0) return@dialog validationResult

            val player = UnityPlayer.createRemotePlayer(hostAddress, port)
            synchronized(listModelLock) {
                listModel.addElement(player)
                list.selectedIndex = listModel.size() - 1
                return@dialog emptyList<ValidationInfo>()
            }
        }
        dialog.showAndGet()
    }
}

class UnityProcessCellRenderer : ColoredListCellRenderer<UnityPlayer>() {
    override fun customizeCellRenderer(list: JList<out UnityPlayer>, player: UnityPlayer?, index: Int, selected: Boolean, hasFocus: Boolean) {
        player ?: return
        if (!player.allowDebugging) {
            append(player.id, SimpleTextAttributes.GRAYED_BOLD_ATTRIBUTES)
            if (player.projectName != null) {
                append(" - ${player.projectName}", SimpleTextAttributes.GRAYED_BOLD_ATTRIBUTES)
            }
            append(" (Debugging disabled)", SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES)
            append(" ${player.host}:${player.debuggerPort}", SimpleTextAttributes.GRAYED_ATTRIBUTES)
        }
        else {
            append(player.id, SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES)
            if (player.projectName != null) {
                append(" - ${player.projectName}", SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES)
            }
            append(" ${player.host}:${player.debuggerPort}")
        }
    }
}