package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.progress.ProgressIndicator
import com.intellij.openapi.progress.Task
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.openapi.ui.ValidationInfo
import com.intellij.ui.*
import com.intellij.ui.components.dialog
import com.intellij.ui.layout.panel
import com.intellij.ui.speedSearch.SpeedSearchUtil
import com.intellij.ui.treeStructure.Tree
import com.jetbrains.rd.util.lifetime.Lifetime
import java.awt.Dimension
import java.awt.Insets
import java.awt.event.KeyEvent
import java.awt.event.MouseEvent
import java.lang.Integer.max
import javax.swing.*
import javax.swing.tree.*

class UnityProcessPickerDialog(private val project: Project) : DialogWrapper(project) {

    data class UnityPlayerModel(val player: UnityPlayer, val debuggerAttached: Boolean)

    private val treeModel = DefaultTreeModel(DefaultMutableTreeNode())
    private val treeModelLock = Object()
    private val tree: Tree
    private val peerPanel: JPanel
    private var busyCount = 0

    init {
        title = "Searching for Unity Editors and Players..."

        tree = Tree().apply {
            model = treeModel
            isRootVisible = false
            showsRootHandles = false
            cellRenderer = UnityProcessTreeCellRenderer(project)
            selectionModel.selectionMode = TreeSelectionModel.SINGLE_TREE_SELECTION
            selectionModel.addTreeSelectionListener {
                isOKActionEnabled = !isSelectionEmpty
                getSelectedPlayerModel()?.let {
                    isOKActionEnabled = !it.debuggerAttached && it.player.allowDebugging
                }
            }

            registerKeyboardAction(okAction, KeyStroke.getKeyStroke(KeyEvent.VK_ENTER, 0), JComponent.WHEN_FOCUSED)

            TreeSpeedSearch(this, { path -> path.lastPathComponent?.toString() }, true)
                .apply { comparator = SpeedSearchComparator(false) }

            emptyText.text = "Searching"
        }

        object: DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent): Boolean {
                if (!tree.isSelectionEmpty) {
                    doOKAction()
                }
                return true
            }
        }.installOn(tree)

        peerPanel = panel {
            row { label("Players and Editors:") }
            row { scrollPane(tree) }
            row {
                button("Add player address manually...", actionListener = { enterCustomIp() })
            }
            commentRow("Please ensure both the <i>Development Build</i> and <i>Script Debugging</i> options are checked in Unity's <i>Build Settings</i> dialog. " +
                "Standalone players must be visible to the current network.")
        }.apply { preferredSize = Dimension(650, 450) } // 600x300

        isOKActionEnabled = false
        init()
        setResizable(true)

        myPreferredFocusedComponent = tree
    }

    override fun createCenterPanel() = peerPanel

    override fun createHelpButton(insets: Insets): JButton {
        val button = super.createHelpButton(insets)
        button.isVisible = false
        return button
    }

    override fun doOKAction() {
        if (okAction.isEnabled) {
            val model = getSelectedPlayerModel() ?: return
            val player = model.player
            if (!model.debuggerAttached && player.allowDebugging) {
                UnityRunUtil.attachToUnityProcess(player.host, player.debuggerPort, player.id, project, player.isEditor)
            }
            close(OK_EXIT_CODE)
        }
    }

    override fun show() {
        Lifetime.using { lifetime ->
            object : Task.Backgroundable(project, "Getting list of Unity processes...") {
                override fun run(indicator: ProgressIndicator) {
                    UnityPlayerListener(project, ::setSearching, {
                        val model = UnityPlayerModel(it, UnityRunUtil.isDebuggerAttached(it.host, it.debuggerPort, project))
                        synchronized(treeModelLock) {
                            val root = treeModel.root as DefaultMutableTreeNode
                            root.add(UnityPlayerTreeNode(model))
                            treeModel.reload()
                        }
                    }, {
                        synchronized(treeModelLock) {
                            val node = (treeModel.root as DefaultMutableTreeNode).children().asSequence().first {
                                c -> (c as? UnityPlayerTreeNode)?.model?.player == it
                            } as MutableTreeNode
                            treeModel.removeNodeFromParent(node)
                            treeModel.reload()
                        }
                    }, lifetime)
                }
            }.queue()
            super.show()
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
            synchronized(treeModelLock) {
                val root = treeModel.root as DefaultMutableTreeNode
                val node = UnityPlayerTreeNode(UnityPlayerModel(player, debuggerAttached))
                root.add(node)
                treeModel.reload()
                tree.selectionPath = TreePath(node.path)
            }
            return@dialog emptyList<ValidationInfo>()
        }
        dialog.showAndGet()
    }

    private fun setSearching(flag: Boolean) {
        if (flag) {
            if (++busyCount > 0) {
                tree.setPaintBusy(true)
            }
        }
        else {
            busyCount = max(0, busyCount - 1)
            if (busyCount == 0) {
                tree.setPaintBusy(false)
            }
        }
    }

    private fun getSelectedPlayerModel(): UnityPlayerModel? {
        return (tree.lastSelectedPathComponent as? UnityPlayerTreeNode)?.model
    }

    data class UnityPlayerTreeNode(val model: UnityPlayerModel): DefaultMutableTreeNode(model)

    class UnityProcessTreeCellRenderer(private val project: Project) : ColoredTreeCellRenderer() {
        init {
            // Override default behaviour, so we can match the entire text, including attributed fragments, rather than
            // just the main text fragment
            this.myUsedCustomSpeedSearchHighlighting = true
        }

        override fun customizeCellRenderer(tree: JTree, value: Any, selected: Boolean, expanded: Boolean, leaf: Boolean, row: Int, hasFocus: Boolean) {

            if (value is UnityPlayerTreeNode) {
                val model = value.model
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

            // The default implementation only applies to the main text. We want to highlight across all fragments
            SpeedSearchUtil.applySpeedSearchHighlighting(tree, this, false, selected)
        }
    }
}
