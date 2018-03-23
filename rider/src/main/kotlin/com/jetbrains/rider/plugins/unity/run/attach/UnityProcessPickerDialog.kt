package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.execution.ProgramRunnerUtil
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironmentBuilder
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.PortField
import com.intellij.ui.ScrollPaneFactory
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.components.JBList
import com.intellij.ui.components.dialog
import com.intellij.ui.layout.panel
import com.jetbrains.rider.plugins.unity.util.convertPortToDebuggerPort
import java.awt.*
import javax.swing.*

class UnityProcessPickerDialog(private val project: Project) : DialogWrapper(project) {

    private val listModel = DefaultListModel<UnityPlayer>()
    private val listModelLock = Object()
    private val list = JBList<UnityPlayer>()
    private val peerPanel: JPanel = JPanel()

    init {
        title = "Search for available Unity processes..."
        list.model = listModel
        list.cellRenderer = UnityProcessCellRenderer()
        init()
    }

    override fun createCenterPanel(): JComponent? {
        peerPanel.layout = BoxLayout(peerPanel, BoxLayout.PAGE_AXIS)
        val pane = ScrollPaneFactory.createScrollPane(list)
        pane.preferredSize = Dimension(400, 200)
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
            if (player != null) {
                val port = if (player.debuggerPort != 0) player.debuggerPort else convertPortToDebuggerPort(player.guid)
                val configuration = UnityLocalAttachConfiguration(port, player.host)
                val profile = UnityLocalAttachRunProfile(player.id, configuration)
                val environment = ExecutionEnvironmentBuilder
                        .create(project, DefaultDebugExecutor.getDebugExecutorInstance(), profile)
                        .build()
                ProgramRunnerUtil.executeConfiguration(environment, false, true)
            }
            close(OK_EXIT_CODE)
        }
    }

    override fun show() {
        var unityProcessListener: UnityProcessListener? = null
        try {
            unityProcessListener = UnityProcessListener(
                    { player ->
                        if (player == null || !player.allowDebugging)
                            return@UnityProcessListener
                        synchronized(listModelLock) {
                            listModel.addElement(player)
                        }
                    },
                    { player ->
                        player ?: return@UnityProcessListener
                        synchronized(listModelLock) {
                            listModel.removeElement(player)
                        }
                    })
            super.show()
        } finally {
            unityProcessListener?.close()
        }
    }

    private fun enterCustomIp() {
        val hostField = JTextField("127.0.0.1")
        val portField = PortField(0)

        val panel = panel {
            row("Address:") { hostField() }
            row("Port:") { portField() }
        }

        val dialog = dialog(
                title = "Enter address of remote process",
                panel = panel,
                focusedComponent = hostField,
                project = project,
                parent = peerPanel) {
            val hostAddress = hostField.text
            val port = portField.number
            if (!hostAddress.isNullOrBlank() && port > 0) {
                val player = UnityPlayer(hostAddress, port, 0, port.toLong(), port.toLong(), 0, hostAddress, true, port)
                synchronized(listModelLock) {
                    listModel.addElement(player)
                    list.selectedIndex = listModel.size() - 1
                    return@dialog true
                }
            }

            return@dialog false
        }
        dialog.showAndGet()
    }
}

class UnityProcessCellRenderer : ColoredListCellRenderer<UnityPlayer>() {
    override fun customizeCellRenderer(list: JList<out UnityPlayer>, player: UnityPlayer?, index: Int, selected: Boolean, hasFocus: Boolean) {
        player ?: return
        val port = if (player.debuggerPort != 0) player.debuggerPort else convertPortToDebuggerPort(player.guid)
        append(player.id, SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES)
        append(" (${player.host}:$port)")
    }

}