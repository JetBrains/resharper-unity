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
import com.jetbrains.rd.util.reactive.Signal
import java.awt.Dimension
import java.awt.Insets
import java.awt.event.MouseEvent
import javax.swing.*

class UnityProcessPickerDialog(private val project: Project) : DialogWrapper(project) {

    data class UnityPlayerModel(val player: UnityPlayer, val debuggerAttached: Boolean)

    private val listModel = DefaultListModel<UnityPlayerModel>()
    private val listModelLock = Object()
    private val list: JBList<UnityPlayerModel>
    private val peerPanel: JPanel
    val onOk = Signal<UnityPlayer>()
    val onCancel = Signal<Unit>()

    init {
        title = "Searching for Unity Editors and Players..."

        list = JBList<UnityPlayerModel>().apply {
            model = listModel
            cellRenderer = UnityProcessCellRenderer(project)
            selectionMode = ListSelectionModel.SINGLE_SELECTION
            selectionModel.addListSelectionListener {
                isOKActionEnabled = selectedIndex != -1
                if (selectedIndex != -1) {
                    isOKActionEnabled = !selectedValue.debuggerAttached && selectedValue.player.allowDebugging
                }
            }

            // Mark as always busy, because we're continually listening for remote players
            setPaintBusy(true)
            setEmptyText("Searching")
        }

        object: DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent): Boolean {
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
        }.apply { preferredSize = Dimension(650, 450) }

        isOKActionEnabled = false
        cancelAction.putValue(FOCUSED_ACTION, true)
        init()
        setResizable(true)
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
            val selected = list.selectedValue
            val player = selected.player
            if (selected != null && !selected.debuggerAttached && selected.player.allowDebugging) {
                onOk.fire(player)
            }
            close(OK_EXIT_CODE)
        }
    }

    override fun doCancelAction() {
        onCancel.fire(Unit)
        super.doCancelAction()
    }

    override fun show() {
        val lifetimeDefinition = project.defineNestedLifetime()
        try {
            object : Task.Backgroundable(project, "Getting list of Unity processes...") {
                override fun run(indicator: ProgressIndicator) {
                    UnityPlayerListener(project, {
                        val model = UnityPlayerModel(it, UnityRunUtil.isDebuggerAttached(it.host, it.debuggerPort, project))
                        synchronized(listModelLock) {
                            listModel.addElement(model)
                        }
                    }, {
                        synchronized(listModelLock) {
                            val element = listModel.elements().asSequence().first { e -> e.player == it }
                            listModel.removeElement(element)
                        }
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
            val debuggerAttached = UnityRunUtil.isDebuggerAttached(hostAddress, port, project)
            synchronized(listModelLock) {
                listModel.addElement(UnityPlayerModel(player, debuggerAttached))
                list.selectedIndex = listModel.size() - 1
                return@dialog emptyList<ValidationInfo>()
            }
        }
        dialog.showAndGet()
    }

    class UnityProcessCellRenderer(private val project: Project) : ColoredListCellRenderer<UnityPlayerModel>() {
        override fun customizeCellRenderer(list: JList<out UnityPlayerModel>, model: UnityPlayerModel?, index: Int, selected: Boolean, hasFocus: Boolean) {
            model ?: return
            val player = model.player
            val debug = player.allowDebugging && !UnityRunUtil.isDebuggerAttached(player.host, player.port, project)
            val attributes = if (debug) SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES else SimpleTextAttributes.GRAYED_BOLD_ATTRIBUTES
            append(player.id, attributes)
            if (player.roleName != null) {
                append(" ${player.roleName}", attributes)
            }
            if (player.projectName != null) {
                append(" - ${player.projectName}", attributes)
            }
            if (model.debuggerAttached) {
                append(" (Debugger attached)", SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES)
            }
            if (!player.allowDebugging) {
                append(" (Script Debugging disabled)", SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES)
            }
            append(" ${player.host}:${player.debuggerPort}", SimpleTextAttributes.GRAYED_ATTRIBUTES)
            if (player.pid != null) {
                append(" (pid: ${player.pid})", SimpleTextAttributes.GRAYED_ATTRIBUTES)
            }
        }
    }
}
