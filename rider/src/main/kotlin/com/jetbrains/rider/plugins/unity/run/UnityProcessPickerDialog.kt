package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.DialogWrapper
import com.intellij.ui.*
import com.intellij.ui.components.dialog
import com.intellij.ui.components.noteComponent
import com.intellij.ui.dsl.builder.Align
import com.intellij.ui.dsl.builder.bindIntValue
import com.intellij.ui.dsl.builder.bindText
import com.intellij.ui.dsl.builder.panel
import com.intellij.ui.speedSearch.SpeedSearchUtil
import com.intellij.ui.treeStructure.Tree
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.intellij.util.ui.tree.TreeUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.configurations.attachToUnityProcess
import org.jetbrains.annotations.Nls
import java.awt.Component
import java.awt.Dimension
import java.awt.Graphics
import java.awt.Insets
import java.awt.event.KeyEvent
import java.awt.event.MouseEvent
import javax.swing.*
import javax.swing.tree.*

class UnityProcessPickerDialog(private val project: Project) : DialogWrapper(project) {

    companion object {
        const val UNKNOWN_PROJECTS = "Unknown Projects"
        const val USB_DEVICES = "USB Devices"
    }

    private class UnityProcessTreeNode(val process: UnityProcess, val debuggerAttached: Boolean): DefaultMutableTreeNode()

    private val treeModel = DefaultTreeModel(DefaultMutableTreeNode())
    private val treeModelLock = Object()
    private val tree: Tree
    private val peerPanel: JPanel

    init {
        title = UnityBundle.message("dialog.title.searching")

        tree = Tree().apply {
            model = treeModel
            isRootVisible = false
            showsRootHandles = false
            rowHeight = 0
            toggleClickCount = 0
            cellRenderer = GroupedProcessTreeCellRenderer()
            selectionModel.selectionMode = TreeSelectionModel.SINGLE_TREE_SELECTION
            selectionModel.addTreeSelectionListener {
                isOKActionEnabled = !isSelectionEmpty
                getSelectedUnityProcessTreeNode()?.let {
                    isOKActionEnabled = !it.debuggerAttached && it.process.allowDebugging
                }
            }

            // Make sure the current theme doesn't add a border or insets to the contents of the tree. This can affect
            // the separator, which is drawn as content and non-opaque. If it's shifted, then we'll see the item
            // background around it, which is very obvious when the item is selected. This is not a problem with Rider's
            // default themes, but is with IntelliJ Light and Darcula.
            border = JBUI.Borders.empty()

            registerKeyboardAction(okAction, KeyStroke.getKeyStroke(KeyEvent.VK_ENTER, 0), JComponent.WHEN_FOCUSED)

            TreeSpeedSearch(this, true) {
                path -> path.lastPathComponent?.toString()
            }.apply { comparator = SpeedSearchComparator(false) }

            emptyText.text = UnityBundle.message("dialog.progress.searching")

            // Show that we're always searching. We poll players every second, but that is so fast that we can't show it
            // for the poll duration, so just always show it.
            setPaintBusy(true)
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
            row { scrollCell(tree).align(Align.FILL) }
            row {
                button(UnityBundle.message("button.add.player.address.manually"), actionListener = { enterCustomIp() })
            }
            row {
                comment(UnityBundle.message("please.ensure.both.the.development.build.and.script.debugging.options.are.checked.in.unity.build.settings.dialog"))
            }
        }.apply { preferredSize = Dimension(650, 450) }

        isOKActionEnabled = false
        init()
        isResizable = true

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
            val model = getSelectedUnityProcessTreeNode() ?: return
            val process = model.process
            if (!model.debuggerAttached && process.allowDebugging) {
                attachToUnityProcess(project, process)
            }
            close(OK_EXIT_CODE)
        }
    }

    override fun show() {
        Lifetime.using { lifetime ->
            UnityDebuggableProcessListener(project, lifetime,
                { UIUtil.invokeLaterIfNeeded { addProcess(it) } },
                { UIUtil.invokeLaterIfNeeded { removeProcess(it) } }
            )
            super.show()
        }
    }

    private fun getSelectedUnityProcessTreeNode() =
        tree.lastSelectedPathComponent as? UnityProcessTreeNode

    internal data class CustomPlayerModel(var name: String = "Custom Player",
                                          var host: String = "",
                                          var port: Int = 0)

    private fun enterCustomIp() {
        val model = CustomPlayerModel()
        val panel = panel {
            row { cell(noteComponent(UnityBundle.message("enter.the.ip.address.of.the.unity.process"))) }
            row(UnityBundle.message("name.colon")) {
                textField().bindText(model::name)
                    .errorOnApply(UnityBundle.message("dialog.message.name.must.not.be.empty")) { it.text.isBlank() }
            }
            row(UnityBundle.message("host.colon")) {
                textField().bindText(model::host)
                    .errorOnApply(UnityBundle.message("dialog.message.host.address.must.not.be.empty")) { it.text.isBlank() }
                    .focused()
            }
            row(UnityBundle.message("port.colon")) {
                cell(PortField(0)).bindIntValue(model::port)
                    .errorOnApply(UnityBundle.message("dialog.message.port.number.must.be.positive")) { it.number <= 0 }
            }
        }

        @Suppress("DialogTitleCapitalization") val dialog = dialog(
            title = UnityBundle.message("dialog.title.add.unity.process"),
            panel = panel,
            project = project,
            parent = peerPanel)
        if (dialog.showAndGet()) {
            val process = UnityRemotePlayer(model.name, model.host, model.port, true, project.name)
            val node = addProcess(process)
            tree.selectionPath = TreePath(node.path)
        }
    }

    private fun addProcess(process: UnityProcess): UnityProcessTreeNode {
        val isDebuggerAttached = process is UnityRemoteConnectionDetails && UnityRunUtil.isDebuggerAttached(process.host, process.port, project)

        synchronized(treeModelLock) {
            val root = treeModel.root as DefaultMutableTreeNode

            val newNode = UnityProcessTreeNode(process, isDebuggerAttached)

            if (process is UnityEditorHelper) {
                // Try to add the helper as a child node of the parent editor
                (TreeUtil.findNode(root) {
                    val parentProcess = (it as? UnityProcessTreeNode)?.process ?: return@findNode false
                    parentProcess is UnityEditor && parentProcess.projectName == process.projectName
                } as? UnityProcessTreeNode)?.let {
                    insertChildNode(it, newNode)
                    // Expand the parent
                    tree.expandPath(TreePath(it.path))
                    return newNode
                }

                // If we can't find the parent editor, just drop through and insert as normal a top level node, grouped
                // by project (editor helpers will always have a project)
            }

            insertChildNode(root, newNode)

            // Reparent any editor helpers that were either "orphaned" or simply listed first
            if (process is UnityEditor) {
                root.children().asSequence().filterIsInstance<UnityProcessTreeNode>().filter {
                    it.process is UnityEditorHelper && it.process.projectName == newNode.process.projectName
                }.forEach {
                    treeModel.removeNodeFromParent(it)
                    treeModel.insertNodeInto(it, newNode, 0)
                }
                tree.expandPath(TreePath(newNode.path))
            }

            // Make sure the root node is expanded
            tree.expandPath(TreePath(root.path))

            // We have variable height rows to allow for the "separators". However, the tree's UI class caches row
            // heights for each cell. This causes problems when an item with a separator no longer needs the separator.
            // There are no public APIs to invalidate the cache. We could reload the model, but that collapses any open
            // nodes, so we'd have to expand everything again. So, let's change the row height, and then immediately
            // change it back to 0 (to indicate variable height). This invalidates the height cache, but maintains all
            // other state.
            tree.rowHeight = 1
            tree.rowHeight = 0

            return newNode
        }
    }

    private fun insertChildNode(root: DefaultMutableTreeNode, newNode: UnityProcessTreeNode) {
        val comparator = TreeNodeComparator(project.name)
        val index = root.children().asSequence().indexOfFirst {
            comparator.compare(newNode, it) < 0 // newNode is less than it, so should be higher up in the list
        }
        treeModel.insertNodeInto(newNode, root, if (index == -1) root.childCount else index)
    }

    private fun removeProcess(process: UnityProcess) {
        synchronized(treeModelLock) {
            TreeUtil.findNode(treeModel.root as DefaultMutableTreeNode) {
                (it as? UnityProcessTreeNode)?.process == process
            }?.let {
                treeModel.removeNodeFromParent(it)

                // Now reparent any children until they're removed
                it.children().asSequence().filterIsInstance<UnityProcessTreeNode>().forEach { child ->
                    treeModel.removeNodeFromParent(child)
                    insertChildNode(treeModel.root as DefaultMutableTreeNode, child)
                }
            }
        }
    }

    private class TreeNodeComparator(private val projectName: String) : Comparator<TreeNode> {
        override fun compare(o1: TreeNode, o2: TreeNode): Int {
            val node1 = o1 as? UnityProcessTreeNode ?: return -1
            val node2 = o2 as? UnityProcessTreeNode ?: return 1

            val projectName1 = node1.process.projectName ?: UNKNOWN_PROJECTS
            val projectName2 = node2.process.projectName ?: UNKNOWN_PROJECTS

            // Sort by project name
            var x = when {
                projectName1 == projectName2 -> 0
                projectName1 == projectName -> -1
                projectName2 == projectName -> 1
                projectName1 == USB_DEVICES -> -1
                projectName2 == USB_DEVICES -> 1
                projectName1 == UNKNOWN_PROJECTS -> -1
                projectName2 == UNKNOWN_PROJECTS -> 1
                else -> projectName1.compareTo(projectName2, true)
            }
            if (x != 0) return x

            // Then by process kind
            x = getProcessKindWeight(o1.process).compareTo(getProcessKindWeight(o2.process))
            if (x != 0) return x

            // Then by display name
            return o1.process.displayName.compareTo(o2.process.displayName, true)
        }

        private fun getProcessKindWeight(process: UnityProcess): Int {
            // Players will be grouped by project, then sorted by their relative weights. The editor is considered the
            // most important, so goes first. Editor helpers are grouped with their project's editors when possible, and
            // if not, appear below any other editors. Locally connected devices are deemed the next most important,
            // followed by players running locally on the desktop. Custom players have been entered by a user, so should
            // be considered more important than remote players, which could simply be other team members running the
            // game on the same network.
            return when (process) {
                is UnityEditor -> 10
                is UnityEditorHelper -> 20          // This is handled as a child node of UnityEditor
                is UnityIosUsbProcess, is UnityAndroidAdbProcess -> 30  // These are put into their own group
                is UnityLocalPlayer -> 40
                is UnityRemotePlayer -> 50
            }
        }
    }

    // This cell renderer contains several components:
    // * GroupedElementsRenderer.MyComponent (myRendererComponent) - root component
    //   * SeparatorWithText (mySeparatorComponent)
    //   * SimpleColoredComponent (myComponent) - item component defined in this derived class
    // * ErrorLabel (myTextLabel) - appears to be a text label for accessibility
    // The root myRendererComponent is drawn in the correct selected/unselected tree background
    private class GroupedProcessTreeCellRenderer : GroupedElementsRenderer.Tree() {
        override fun getTreeCellRendererComponent(tree: JTree, value: Any?, selected: Boolean, expanded: Boolean, leaf: Boolean, row: Int, hasFocus: Boolean): Component {

            val itemComponent = itemComponent as SimpleColoredComponent
            itemComponent.clear()

            val node = value as? UnityProcessTreeNode ?: return myRendererComponent

            val unityProcess = node.process
            val attributes = if (!node.debuggerAttached) SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES else SimpleTextAttributes.GRAYED_BOLD_ATTRIBUTES
            val projectName = unityProcess.projectName ?: if (unityProcess is UnityIosUsbProcess || unityProcess is UnityAndroidAdbProcess) USB_DEVICES else UNKNOWN_PROJECTS
            val hasSeparator = !isChildProcess(node) && (isFirstItem(node) || getPreviousSiblingProjectName(node) != projectName)

            // Set up visibility and selected status. This does not (re)set the selected status of the returned
            // myRendererComponent, which has its background set directly by the tree
            val component = configureComponent("", "", null, null, selected, hasSeparator, projectName, -1)
            setDeselected(component)

            val focused = tree.hasFocus()
            if (unityProcess is UnityEditorHelper && unityProcess.roleName.isNotEmpty()) {
                append(itemComponent, unityProcess.roleName, attributes, selected, focused, true)
            } else {
                append(itemComponent, unityProcess.displayName, attributes, selected, focused, true)
            }
            if (node.debuggerAttached) {
                append(itemComponent, UnityBundle.message("appended.debugger.attached"), SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES, selected, focused)
            }
            if (!unityProcess.allowDebugging) {
                append(itemComponent, UnityBundle.message("appended.script.debugging.disabled"), SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES, selected, focused)
            }
            if (unityProcess is UnityRemoteConnectionDetails) {
                append(itemComponent, " ${unityProcess.host}:${unityProcess.port}", SimpleTextAttributes.GRAYED_ATTRIBUTES, selected, focused)
            }
            if (unityProcess is UnityLocalProcess) {
                append(itemComponent, UnityBundle.message("appended.pid.0", unityProcess.pid.toString()), SimpleTextAttributes.GRAYED_ATTRIBUTES, selected, focused)
            }
            if (unityProcess is UnityAndroidAdbProcess && unityProcess.packageName != null) {
                append(itemComponent, UnityBundle.message("appended.android.package", unityProcess.packageName), SimpleTextAttributes.GRAYED_ATTRIBUTES, selected, focused)
            }

            SpeedSearchUtil.applySpeedSearchHighlighting(tree, itemComponent, true, selected)

            return component
        }

        override fun createSeparator(): SeparatorWithText {
            // The separator is not painted opaque by default but looks bad painted over a selected background
            return object: SeparatorWithText() {
                override fun paint(g: Graphics?) {
                    g?.color = background
                    g?.fillRect(0, 0, width, height)
                    super.paint(g)
                }
            }
        }

        override fun createItemComponent(): JComponent {
            myTextLabel = ErrorLabel() // dummy component required by base class for accessibility

            // Don't paint the background for the item component because we don't have a way of setting the unfocused
            // selected colour. If we paint as non-opaque, we get myRendererComponent's background
            return SimpleColoredComponent().apply { isOpaque = false }
        }

        private fun isFirstItem(node: UnityProcessTreeNode) = node.previousSibling == null
        private fun isChildProcess(node: UnityProcessTreeNode) = node.parent is UnityProcessTreeNode

        private fun getPreviousSiblingProjectName(node: UnityProcessTreeNode): String {
            return (node.previousSibling as? UnityProcessTreeNode)?.let {
                if (it.process is UnityIosUsbProcess || it.process is UnityAndroidAdbProcess) {
                    USB_DEVICES
                }
                else {
                    it.process.projectName
                }
            } ?: UNKNOWN_PROJECTS
        }

        /**
         * When the item is selected, then we use the tree's default selection foreground.
         * It guaranties readability of the selected text in any LAF.
         */
        private fun append(component: SimpleColoredComponent, @Nls fragment: String, attributes: SimpleTextAttributes, isSelected: Boolean, isFocused: Boolean, isMainText: Boolean = false) {
            if (isSelected && isFocused) {
                component.append(fragment, SimpleTextAttributes(attributes.style, UIUtil.getTreeSelectionForeground(true)), isMainText)
            } else {
                component.append(fragment, attributes, isMainText)
            }
        }
    }
}
