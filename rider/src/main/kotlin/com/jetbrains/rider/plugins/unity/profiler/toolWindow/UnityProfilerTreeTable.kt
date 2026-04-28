package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ToggleAction
import com.intellij.openapi.application.runInEdt
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.ui.DoubleClickListener
import com.intellij.ui.PopupHandler
import com.intellij.ui.treeStructure.treetable.ListTreeTableModelOnColumns
import com.intellij.ui.treeStructure.treetable.TreeTable
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.tree.TreeUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.intersect
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesDaemon
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerTreeViewModel
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import java.awt.Component
import java.awt.event.KeyEvent
import java.awt.event.MouseEvent
import javax.swing.KeyStroke
import javax.swing.RowSorter
import javax.swing.SwingUtilities
import javax.swing.table.TableCellRenderer
import javax.swing.table.TableColumn
import javax.swing.table.TableModel
import javax.swing.tree.DefaultMutableTreeNode
import javax.swing.tree.TreePath

class UnityProfilerTreeTable(
    private val viewModel: UnityProfilerTreeViewModel,
    private val project: Project,
    lifetime: Lifetime
) : TreeTable(ListTreeTableModelOnColumns(DefaultMutableTreeNode(), UnityProfilerColumns.allColumns)) {

    // For each currently-collapsed path, the descendants that were expanded at the
    // moment of collapse. Captured in processMouseEvent before super dispatches to the
    // collapse — this widget cascades collapse for nodes with expanded descendants and
    // skips TreeWillExpandListener entirely, so the snapshot has to happen pre-cascade.
    // Replayed in processMouseEvent right after super, when the matching click re-expands
    // the path. Cleared on snapshot change.
    private val savedExpansionState = mutableMapOf<TreePath, List<TreePath>>()

    init {
        setRootVisible(false)
        // Disable double-click toggles the row.
        tree.toggleClickCount = 0
        val sorter = UnityProfilerRowSorter()
        rowSorter = sorter
        viewModel.activeSortColumn.advise(lifetime) { sorter.update() }
        viewModel.activeSortOrder.advise(lifetime) { sorter.update() }
        viewModel.visibleColumns.advise(lifetime) { updateColumnsVisibility() }

        addMouseListener(object : PopupHandler() {
            override fun invokePopup(comp: Component, x: Int, y: Int) = showPopupMenu(comp, x, y)

            override fun mouseClicked(e: MouseEvent) {
                super.mouseClicked(e)
                project.service<UnityProfilerUsagesDaemon>().incrementTreeInteraction()
            }
        })

        tableHeader.addMouseListener(object : PopupHandler() {
            override fun invokePopup(comp: Component, x: Int, y: Int) {
                val actionGroup = DefaultActionGroup()
                for (column in UnityProfilerSortColumn.entries) {
                    if (column == UnityProfilerSortColumn.NAME) continue
                    actionGroup.add(object : ToggleAction(column.column.name) {
                        override fun isSelected(e: AnActionEvent): Boolean = viewModel.visibleColumns.value.contains(column)
                        override fun setSelected(e: AnActionEvent, state: Boolean) {
                            viewModel.toggleColumnVisibility(column)
                        }

                        override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
                    })
                }
                ActionManager.getInstance()
                    .createActionPopupMenu(UnityToolWindowFactory.ACTION_PLACE, actionGroup)
                    .component
                    .show(comp, x, y)
            }
        })
        object : DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent): Boolean {
                return navigateToRow(rowAtPoint(event.point))
            }
        }.installOn(this)

        registerKeyboardAction({
            navigateToRow(selectedRow)
        }, KeyStroke.getKeyStroke(KeyEvent.VK_ENTER, 0), WHEN_FOCUSED)

        viewModel.treeRoot.advise(viewModel.lifetime.intersect(lifetime)) { root ->
            runInEdt {
                val selected = tree.selectionPaths ?: emptyArray()

                // A new snapshot replaces the root with freshly-built nodes — saved
                // children state from the previous tree can't apply, drop it.
                savedExpansionState.clear()

                (tableModel as ListTreeTableModelOnColumns).setRoot(root ?: DefaultMutableTreeNode())

                if (root != null) {
                    val filter = viewModel.filterState.value
                    if (filter.text.isEmpty()) {
                        expandDefaultNodes(root)
                    } else {
                        expandToMatches(root, filter.text, filter.mode)
                    }
                }

                if (selected.isNotEmpty()) {
                    tree.selectionPaths = selected
                }

                updateColumnSizes()
            }
        }
    }

    override fun processMouseEvent(e: MouseEvent) {
        if (e.id == MouseEvent.MOUSE_PRESSED) {
            val row = rowAtPoint(e.point)
            val path = if (row >= 0) tree.getPathForRow(row) else null
            if (path != null) {
                // Alt+Left-Click: recursively expand a collapsed node, or collapse an
                // expanded node and reset saved children state under it (so a
                // subsequent regular re-expand shows descendants as collapsed).
                if (SwingUtilities.isLeftMouseButton(e) && e.isAltDown && !e.isPopupTrigger) {
                    if (tree.isExpanded(path)) {
                        savedExpansionState.keys.removeIf { path.isDescendant(it) }
                        tree.collapsePath(path)
                    } else {
                        expandSubtree(path)
                    }
                    setRowSelectionInterval(row, row)
                    e.consume()
                    return
                }

                // Regular click: snapshot expanded descendants before the cascade
                // collapse clears them; restore them after re-expand.
                val wasExpanded = tree.isExpanded(path)
                if (wasExpanded) {
                    savedExpansionState[path] = TreeUtil.collectExpandedPaths(tree, path)
                }

                super.processMouseEvent(e)

                if (!wasExpanded && tree.isExpanded(path)) {
                    savedExpansionState.remove(path)?.let { saved ->
                        TreeUtil.restoreExpandedPaths(tree, saved)
                    }
                }
                return
            }
        }
        super.processMouseEvent(e)
    }

    override fun isTreeColumn(column: Int): Boolean = convertColumnIndexToModel(column) == UnityProfilerSortColumn.NAME.ordinal

    private fun navigateToRow(row: Int): Boolean {
        if (row < 0) return false
        val node = tree.getPathForRow(row)?.lastPathComponent as? DefaultMutableTreeNode ?: return false
        viewModel.navigate(node)
        return true
    }

    private fun updateColumnsVisibility() {
        val visible = viewModel.visibleColumns.value
        val targetModelIndices = UnityProfilerSortColumn.entries
            .filter { visible.contains(it) }
            .map { it.ordinal }

        val currentVisible = (0 until columnModel.columnCount).map { columnModel.getColumn(it) }
        val currentModelIndices = currentVisible.map { it.modelIndex }

        if (currentModelIndices == targetModelIndices) return

        currentVisible.forEach { columnModel.removeColumn(it) }
        for (modelIndex in targetModelIndices) {
            val tableColumn = TableColumn(modelIndex)
            tableColumn.headerValue = UnityProfilerSortColumn.entries[modelIndex].column.name
            columnModel.addColumn(tableColumn)
        }

        updateColumnSizes()
    }

    override fun getCellRenderer(row: Int, column: Int): TableCellRenderer {
        val modelColumn = convertColumnIndexToModel(column)
        val columnInfo = (tableModel as ListTreeTableModelOnColumns).columnInfos[modelColumn]
        val node = tree.getPathForRow(row)?.lastPathComponent
        val baseRenderer = columnInfo.getRenderer(node) ?: super.getCellRenderer(row, column)
        return columnInfo.getCustomizedRenderer(node, baseRenderer)
    }

    fun updateColumnSizes() {
        val header = tableHeader
        val defaultRenderer = header?.defaultRenderer

        val columnInfos = (tableModel as ListTreeTableModelOnColumns).columnInfos
        for (i in 0 until columnModel.columnCount) {
            val column = columnModel.getColumn(i)
            val modelIndex = column.modelIndex
            if (modelIndex >= columnInfos.size) continue
            val columnInfo = columnInfos[modelIndex]

            val headerComponent = defaultRenderer?.getTableCellRendererComponent(this, column.headerValue, false, false, 0, 0)
            val headerSize = headerComponent?.preferredSize ?: JBUI.emptySize()
            val maxStringValue: String?
            val preferredValue: String?
            if (columnInfo.getWidth(this) > 0) {
                val width = columnInfo.getWidth(this)
                column.maxWidth = width
                column.preferredWidth = width
                column.minWidth = width
            } else if (columnInfo.maxStringValue.let { maxStringValue = it; it != null }) {
                var width = getFontMetrics(font).stringWidth(maxStringValue) + columnInfo.additionalWidth
                width = Math.max(width, headerSize.width)
                column.preferredWidth = width
                column.maxWidth = width
            } else if (columnInfo.preferredStringValue.let { preferredValue = it; it != null }) {
                var width = getFontMetrics(font).stringWidth(preferredValue) + columnInfo.additionalWidth
                width = Math.max(width, headerSize.width)
                column.preferredWidth = width
            }
        }
    }

    private fun expandSubtree(path: TreePath) {
        val node = path.lastPathComponent as? DefaultMutableTreeNode ?: return
        val paths = mutableListOf(path)
        collectDescendantPaths(node, path, paths)
        TreeUtil.expandPaths(tree, paths)
    }

    private fun collectDescendantPaths(node: DefaultMutableTreeNode, parentPath: TreePath, result: MutableList<TreePath>) {
        node.children().asSequence()
            .filterIsInstance<DefaultMutableTreeNode>()
            .filter { it.childCount > 0 }
            .forEach { child ->
                val childPath = parentPath.pathByAddingChild(child)
                result.add(childPath)
                collectDescendantPaths(child, childPath, result)
            }
    }

    private fun expandDefaultNodes(root: DefaultMutableTreeNode) {
        val rootPath = TreeUtil.getPathFromRoot(root)
        tree.expandPath(rootPath)
        root.children().asSequence()
            .filterIsInstance<DefaultMutableTreeNode>()
            .filter { it.childCount > 0 }
            .forEach { tree.expandPath(rootPath.pathByAddingChild(it)) }
    }

    private fun expandToMatches(node: DefaultMutableTreeNode, pattern: String, filterMatchMode: FilterMatchMode) {
        val name = node.nodeData?.name ?: ""
        val matches = filterMatchMode.matches(name, pattern)

        if (matches) {
            val path = TreeUtil.getPathFromRoot(node)
            // expandPath is a no-op for leaf nodes, so expand parent to ensure leaf matches are visible
            val parentPath = path.parentPath
            if (parentPath != null) {
                tree.expandPath(parentPath)
            }
            tree.expandPath(path)
        }

        node.children().asSequence()
            .filterIsInstance<DefaultMutableTreeNode>()
            .forEach { expandToMatches(it, pattern, filterMatchMode) }
    }

    private fun showPopupMenu(comp: Component, x: Int, y: Int) {
        val row = rowAtPoint(java.awt.Point(x, y))
        if (row < 0) return
        val node = tree.getPathForRow(row)?.lastPathComponent as? DefaultMutableTreeNode ?: return
        val nodeData = node.nodeData ?: return

        val actionGroup = DefaultActionGroup()
        actionGroup.add(object : AnAction(UnityUIBundle.message("unity.profiler.toolwindow.jump.to.source")) {
            override fun actionPerformed(e: AnActionEvent) {
                viewModel.navigate(node)
            }

            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
        })
        actionGroup.addSeparator()
        actionGroup.add(object : AnAction(UnityUIBundle.message("unity.profiler.toolwindow.filter.by", nodeData.name)) {
            override fun actionPerformed(e: AnActionEvent) {
                viewModel.setFilter(nodeData.name, FilterMatchMode.EXACT)
            }

            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
        })

        if (viewModel.filterState.value.text.isNotEmpty()) {
            actionGroup.add(object : AnAction(UnityUIBundle.message("unity.profiler.toolwindow.clear.filter")) {
                override fun actionPerformed(e: AnActionEvent) {
                    viewModel.setFilter("", FilterMatchMode.CONTAINS)
                }

                override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
            })
        }

        ActionManager.getInstance()
            .createActionPopupMenu(UnityToolWindowFactory.ACTION_PLACE, actionGroup)
            .component
            .show(comp, x, y)
    }

    private inner class UnityProfilerRowSorter : RowSorter<TableModel>() {
        override fun getModel(): TableModel = this@UnityProfilerTreeTable.model

        override fun toggleSortOrder(column: Int) {
            val sortColumn = UnityProfilerSortColumn.entries.getOrNull(column) ?: return
            viewModel.changeSort(sortColumn)
        }

        override fun convertRowIndexToView(index: Int): Int = index
        override fun convertRowIndexToModel(index: Int): Int = index

        override fun getSortKeys(): List<SortKey> {
            return listOf(SortKey(viewModel.activeSortColumn.value.ordinal, viewModel.activeSortOrder.value))
        }

        override fun setSortKeys(keys: List<SortKey>?) {
        }

        override fun getViewRowCount(): Int = model.rowCount
        override fun getModelRowCount(): Int = model.rowCount

        override fun modelStructureChanged() {}
        override fun allRowsChanged() {}
        override fun rowsInserted(firstRow: Int, endRow: Int) {}
        override fun rowsDeleted(firstRow: Int, endRow: Int) {}
        override fun rowsUpdated(firstRow: Int, endRow: Int) {}
        override fun rowsUpdated(firstRow: Int, endRow: Int, column: Int) {}

        fun update() {
            fireSortOrderChanged()
        }
    }
}
