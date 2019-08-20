package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.progress.Task
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.defineNestedLifetime
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.openapi.ui.ValidationInfo
import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.DoubleClickListener
import com.intellij.ui.PortField
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.components.JBList
import com.intellij.ui.components.dialog
import com.intellij.ui.layout.panel
import java.awt.Dimension
import java.awt.Insets
import java.awt.event.MouseEvent
import javax.swing.*

class UnityProcessPickerDialog(private val project: Project) : DialogWrapper(project) {

    private val listModel = DefaultListModel<UnityPlayer>()
    private val listModelLock = Object()
    private val list: JBList<UnityPlayer>
    private val peerPanel: JPanel

    init {
        title = "Searching for Unity Editors and Players..."

        list = JBList<UnityPlayer>().apply {
            model = listModel
            cellRenderer = UnityProcessCellRenderer()
            selectionMode = ListSelectionModel.SINGLE_SELECTION
            selectionModel.addListSelectionListener {
                isOKActionEnabled = selectedIndex != -1
                if (selectedIndex != -1) {
                    isOKActionEnabled = selectedValue.allowDebugging
                }
            }

            // Mark as always busy, because we're continually listening for remote players
            setPaintBusy(true)
            setEmptyText("Searching")
        }

        object: DoubleClickListener() {
            override fun onDoubleClick(p0: MouseEvent?): Boolean {
                if (list.selectedIndex != -1) {
                    doOKAction()
                }
                return true
            }
        }.installOn(list)

        peerPanel = panel {
            row { scrollPane(list) }
            row {
                button("Add player address manually...", actionListener = { enterCustomIp() })
            }
            commentRow("Please ensure both the <i>Development Build</i> and <i>Script Debugging</i> options are checked in Unity's <i>Build Settings</i> dialog. " +
                "Standalone players must be visible to the current network.")
        }.apply { minimumSize = Dimension(650, 300) }

        isOKActionEnabled = false
        cancelAction.putValue(FOCUSED_ACTION, true)
        init()
        setResizable(false)
    }

    // DialogWrapper only lets the Mac set the preferred component via FOCUSED_ACTION because reasons
    override fun getPreferredFocusedComponent(): JComponent? = myPreferredFocusedComponent
    override fun createCenterPanel() = peerPanel

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
        val lifetimeDefinition = project.defineNestedLifetime()
        try {
            object : Task.Backgroundable(project, "Getting list of Unity processes...") {
                override fun run(indicator: ProgressIndicator) {
                    UnityPlayerListener({
                        synchronized(listModelLock) { listModel.addElement(it) }
                    }, {
                        synchronized(listModelLock) { listModel.removeElement(it) }
                    }, lifetimeDefinition.lifetime)
                }
            }.queue()
            super.show()
        }
        finally {
            lifetimeDefinition.terminate()
        }
    }

    private fun enterCustomIp() {
        val hostField = JTextField("127.0.0.1")
        val portField = PortField(0)

        val panel = panel {
            noteRow("Enter the IP address of the Unity process")
            row("Host:") { hostField().focused() }
            row("Port:") { portField() }
        }

        val dialog = dialog(
                title = "Add Unity process",
                panel = panel,
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
        val attributes = if (player.allowDebugging) SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES else SimpleTextAttributes.GRAYED_BOLD_ATTRIBUTES
        append(player.id, attributes)
        if (player.projectName != null) {
            append(" - ${player.projectName}", attributes)
        }
        if (!player.allowDebugging) {
            append(" (Script Debugging disabled)", SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES)
        }
        append(" ${player.host}:${player.debuggerPort}", SimpleTextAttributes.GRAYED_ATTRIBUTES)
    }
}