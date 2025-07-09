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
        val UNKNOWN_PROJECT = UnityBundle.message("project.name.unknown.project")
        val USB_DEVICES = UnityBundle.message("project.name.usb.devices")
        val CUSTOM_PLAYER_PROJECT = UnityBundle.message("project.name.custom")
    }

    private class UnityProcessTreeNode(val process: UnityProcess, val debuggerAttached: Boolean) : DefaultMutableTreeNode()

    private val treeModel = DefaultTreeModel(DefaultMutableTreeNode())
    private val treeModelLock = Object()
    private val tree: Tree
    private val panel: JPanel

    init {
        title = UnityBundle.message("dialog.title.searching")

        tree = object : Tree() {
            // The default JBViewport implementation to get the size of the Scrollable uses the height of the first
            // getVisibleRowCount rows if they exist, or getVisibleRowCount * height of the first row otherwise.
            // Unfortunately, our first row always has a separator, which forces the dialog to resize when adding a node
            override fun getPreferredScrollableViewportSize(): Dimension {
                val rowHeight = JBUI.CurrentTheme.Tree.rowHeight()
                return Dimension(preferredSize.width, rowHeight * getVisibleRowCount())
            }
        }.apply {
            model = treeModel
            isRootVisible = false
            showsRootHandles = false
            rowHeight = 0
            visibleRowCount = 15
            toggleClickCount = 0
            cellRenderer = GroupedProcessTreeCellRenderer()
            selectionModel.selectionMode = TreeSelectionModel.SINGLE_TREE_SELECTION
            selectionModel.addTreeSelectionListener {
                isOKActionEnabled = !isSelectionEmpty
                getSelectedUnityProcessTreeNode()?.let {
                    isOKActionEnabled = !it.debuggerAttached && it.process.debuggingEnabled
                }
            }

            // Make sure the current theme doesn't add a border or insets to the contents of the tree. This can affect
            // the separator, which is drawn as content and non-opaque. If it's shifted, then we'll see the item
            // background around it, which is very obvious when the item is selected. This is not a problem with Rider's
            // default themes, but is with IntelliJ Light and Darcula.
            border = JBUI.Borders.empty()

            registerKeyboardAction(okAction, KeyStroke.getKeyStroke(KeyEvent.VK_ENTER, 0), JComponent.WHEN_FOCUSED)

            TreeSpeedSearch.installOn(this, true) { path ->
                path.lastPathComponent?.toString()
            }.apply { comparator = SpeedSearchComparator(false) }

            emptyText.text = UnityBundle.message("dialog.progress.searching")

            // Show that we're always searching. We poll players every second, but that is so fast that we can't show it
            // for the poll duration, so just always show it.
            setPaintBusy(true)
        }

        object : DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent): Boolean {
                if (!tree.isSelectionEmpty) {
                    doOKAction()
                }
                return true
            }
        }.installOn(tree)

        panel = panel {
            row { scrollCell(tree).align(Align.FILL) }.resizableRow()
            row {
                button(UnityBundle.message("button.add.player.address.manually"), actionListener = { enterCustomIp() })
            }
            row {
                comment(UnityBundle.message(
                    "please.ensure.both.the.development.build.and.script.debugging.options.are.checked.in.unity.build.settings.dialog"))
            }
        }

        isOKActionEnabled = false
        init()
        isResizable = true

        myPreferredFocusedComponent = tree
    }

    override fun createCenterPanel() = panel

    override fun createHelpButton(insets: Insets): JButton {
        val button = super.createHelpButton(insets)
        button.isVisible = false
        return button
    }

    override fun doOKAction() {
        if (okAction.isEnabled) {
            val model = getSelectedUnityProcessTreeNode() ?: return
            val process = model.process
            if (!model.debuggerAttached && process.debuggingEnabled) {
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

    internal data class CustomPlayerModel(var name: String,
                                          var host: String = "",
                                          var port: Int = 0)

    private fun enterCustomIp() {
        var name = UnityBundle.message("custom.player.default.name")
        var count = 1
        while (findCustomPlayerByName(name) != null) {
            name = UnityBundle.message("custom.player.default.template", count++)
        }
        val model = CustomPlayerModel(name)
        val panel = panel {
            row { cell(noteComponent(UnityBundle.message("enter.the.ip.address.of.the.unity.process"))) }
            row(UnityBundle.message("name.colon")) {
                textField().bindText(model::name)
                    .errorOnApply(UnityBundle.message("dialog.message.name.must.not.be.empty")) { it.text.isBlank() }
                    .errorOnApply(UnityBundle.message("dialog.message.name.must.be.unique")) {
                        findCustomPlayerByName(it.text) != null
                    }
            }
            row(UnityBundle.message("host.colon")) {
                textField().bindText(model::host)
                    .errorOnApply(UnityBundle.message("dialog.message.host.address.must.not.be.empty")) { it.text.isBlank() }
                    .focused()
            }
            row(UnityBundle.message("port.colon")) {
                cell(PortField()).bindIntValue(model::port)
            }
        }

        @Suppress("DialogTitleCapitalization") val dialog = dialog(
            title = UnityBundle.message("dialog.title.add.unity.process"),
            panel = panel,
            project = project,
            parent = this.panel)
        if (dialog.showAndGet()) {
            val process = UnityCustomPlayer(model.name, model.host, model.port, CUSTOM_PLAYER_PROJECT)
            val node = addProcess(process)
            tree.selectionPath = TreePath(node.path)
        }
    }

    private fun findCustomPlayerByName(name: String): UnityCustomPlayer? {
        val root = treeModel.root as DefaultMutableTreeNode
        val node = TreeUtil.findNode(root) {
            ((it as? UnityProcessTreeNode)?.process as? UnityCustomPlayer)?.displayName == name
        } as? UnityProcessTreeNode
        return node?.process as? UnityCustomPlayer
    }

    private fun addProcess(process: UnityProcess): UnityProcessTreeNode {
        val isDebuggerAttached = UnityRunUtil.isDebuggerAttached(process.host, process.port, project)

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

    private class TreeNodeComparator(private val currentProjectName: String) : Comparator<TreeNode> {
        override fun compare(o1: TreeNode, o2: TreeNode): Int {
            val node1 = o1 as? UnityProcessTreeNode ?: return -1
            val node2 = o2 as? UnityProcessTreeNode ?: return 1

            val projectName1 = node1.process.projectName ?: UNKNOWN_PROJECT
            val projectName2 = node2.process.projectName ?: UNKNOWN_PROJECT

            // Sort by project name
            var x = when {
                projectName1 == projectName2 -> 0
                projectName1 == currentProjectName -> -1
                projectName2 == currentProjectName -> 1
                projectName1 == CUSTOM_PLAYER_PROJECT -> -1
                projectName2 == CUSTOM_PLAYER_PROJECT -> 1
                projectName1 == USB_DEVICES -> -1
                projectName2 == USB_DEVICES -> 1
                projectName1 == UNKNOWN_PROJECT -> -1
                projectName2 == UNKNOWN_PROJECT -> 1
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
                is UnityEditorEntryPoint -> 5
                is UnityEditorEntryPointAndPlay -> 7
                is UnityEditor -> 10
                is UnityEditorHelper -> 20          // A child node of UnityEditor
                is UnityVirtualPlayer -> 25
                is UnityIosUsbProcess, is UnityAndroidAdbProcess -> 30  // These are put into their own group
                is UnityLocalPlayer -> 40
                is UnityCustomPlayer -> 45
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
        override fun getTreeCellRendererComponent(tree: JTree,
                                                  value: Any?,
                                                  selected: Boolean,
                                                  expanded: Boolean,
                                                  leaf: Boolean,
                                                  row: Int,
                                                  hasFocus: Boolean): Component {

            val itemComponent = itemComponent as SimpleColoredComponent
            itemComponent.clear()

            val node = value as? UnityProcessTreeNode ?: return myRendererComponent

            val unityProcess = node.process
            val attributes = if (!node.debuggerAttached) SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES else SimpleTextAttributes.GRAYED_BOLD_ATTRIBUTES
            val projectName = unityProcess.projectName
                              ?: if (unityProcess is UnityIosUsbProcess || unityProcess is UnityAndroidAdbProcess) USB_DEVICES else UNKNOWN_PROJECT
            val hasSeparator = !isChildProcess(node) && (isFirstItem(node) || getPreviousSiblingProjectName(node) != projectName)

            // Set up visibility and selected status. This does not (re)set the selected status of the returned
            // myRendererComponent, which has its background set directly by the tree
            val component = configureComponent("", "", null, null, selected, hasSeparator, projectName, -1)
            setDeselected(component)

            val focused = tree.hasFocus()
            val displayName = when {
                unityProcess is UnityEditorHelper && unityProcess.roleName.isNotEmpty() -> unityProcess.roleName
                unityProcess is UnityVirtualPlayer && unityProcess.playerName.isNotEmpty() -> unityProcess.playerName
                else -> unityProcess.displayName
            }
            append(itemComponent, displayName, attributes, selected, focused, true)
            if (node.debuggerAttached) {
                append(itemComponent, UnityBundle.message("appended.debugger.attached"), SimpleTextAttributes.GRAYED_ITALIC_ATTRIBUTES,
                       selected, focused)
            }
            if (!unityProcess.debuggingEnabled) {
                append(itemComponent, UnityBundle.message("appended.script.debugging.disabled"),
                       SimpleTextAttributes.GRAY_ITALIC_ATTRIBUTES, selected, focused)
            }
            if (unityProcess !is UnityIosUsbProcess) {
                // Don't show the hardcoded host and port for iOS devices.
                // Arguably, we could do the same for Android, as they're not correct until the port has been forwarded.
                append(itemComponent, " ${unityProcess.host}:${unityProcess.port}", SimpleTextAttributes.GRAYED_ATTRIBUTES, selected,
                       focused)
            }
            if (unityProcess is UnityLocalProcess) {
                append(itemComponent, UnityBundle.message("appended.pid.0", unityProcess.pid.toString()),
                       SimpleTextAttributes.GRAYED_ATTRIBUTES, selected, focused)
            }
            if (unityProcess is UnityAndroidAdbProcess && unityProcess.packageName != null) {
                append(itemComponent, UnityBundle.message("appended.android.package", unityProcess.packageName),
                       SimpleTextAttributes.GRAYED_ATTRIBUTES, selected, focused)
            }

            SpeedSearchUtil.applySpeedSearchHighlighting(tree, itemComponent, true, selected)

            return component
        }

        override fun createSeparator(): SeparatorWithText {
            // The separator is not painted opaque by default, so includes the item's background, which looks bad when
            // the item is selected - as though the separator was selected, too
            return object : SeparatorWithText() {
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
            } ?: UNKNOWN_PROJECT
        }

        /**
         * When the item is selected, then we use the tree's default selection foreground.
         * It guaranties readability of the selected text in any LAF.
         */
        private fun append(component: SimpleColoredComponent,
                           @Nls fragment: String,
                           attributes: SimpleTextAttributes,
                           isSelected: Boolean,
                           isFocused: Boolean,
                           isMainText: Boolean = false) {
            if (isSelected && isFocused) {
                component.append(fragment, SimpleTextAttributes(attributes.style, UIUtil.getTreeSelectionForeground(true)), isMainText)
            }
            else {
                component.append(fragment, attributes, isMainText)
            }
        }
    }
}
