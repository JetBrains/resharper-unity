package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.ui.AnActionButton
import com.intellij.ui.PanelWithButtons
import com.intellij.ui.ToolbarDecorator
import com.intellij.ui.table.JBTable
import java.awt.Dimension
import javax.swing.JButton
import javax.swing.JComponent
import javax.swing.ListSelectionModel
import javax.swing.table.AbstractTableModel

class ProcessesPanel : PanelWithButtons() {

    private var viewModel: UnityAttachToEditorViewModel? = null
    private var table: JBTable? = null

    fun init(viewModel: UnityAttachToEditorViewModel) {
        this.viewModel = viewModel
        initPanel()
    }

    override fun setEnabled(enabled: Boolean) {
        super.setEnabled(enabled)
        table?.isEnabled = enabled
    }

    // Buttons on the side of the panel
    override fun createButtons(): Array<JButton> {
        return emptyArray()
    }

    override fun getLabelText(): String? {
        return "Unity Editor instances"
    }

    override fun createMainComponent(): JComponent {

        val vm = viewModel!!

        val columns = arrayOf("Process ID", "Process Name", "Project Name")

        val dataModel = object : AbstractTableModel() {
            override fun getColumnCount() = columns.count()
            override fun getColumnName(column: Int) = columns[column]
            override fun getColumnClass(columnIndex: Int): Class<*>? {
                return when (columnIndex) {
                    0 -> Integer::class.java
                    1 -> String::class.java
                    2 -> String::class.java
                    else -> null
                }
            }

            override fun getRowCount() = vm.editorProcesses.count()

            override fun getValueAt(rowIndex: Int, columnIndex: Int): Any? {
                return when (columnIndex) {
                    0 -> vm.editorProcesses[rowIndex].pid
                    1 -> vm.editorProcesses[rowIndex].name
                    2 -> vm.editorProcesses[rowIndex].projectName
                    else -> null
                }
            }
        }

        table = JBTable(dataModel)
        with(table!!) {
            setEnableAntialiasing(true)
            emptyText.text = "No Unity Editor instances found"
            preferredScrollableViewportSize = Dimension(150, rowHeight * 6)

            val fontMetrics = tableHeader.getFontMetrics(tableHeader.font)
            var headerWidth = fontMetrics.stringWidth(columns[0])
            getColumn(columns[0]).preferredWidth = headerWidth + 20
            getColumn(columns[0]).maxWidth = headerWidth + 20

            headerWidth = fontMetrics.stringWidth(columns[1])
            getColumn(columns[1]).preferredWidth = headerWidth + 50
            getColumn(columns[1]).maxWidth = headerWidth + 200
            getColumn(columns[2]).preferredWidth = fontMetrics.stringWidth(columns[2])

            setSelectionMode(ListSelectionModel.SINGLE_SELECTION)
            selectionModel.addListSelectionListener {
                if (selectedRow > -1) {
                    vm.pid.value = vm.editorProcesses[selectedRow].pid
                }
            }

            updateSelection(this)

            vm.pid.advise(vm.lifetime) { updateSelection(this) }
        }

        vm.editorProcesses.advise(vm.lifetime) {
            if (it.newValueOpt == null)
                dataModel.fireTableRowsDeleted(it.index, it.index)
            else
                dataModel.fireTableRowsInserted(it.index, it.index)
            updateSelection(table!!)
        }

        return ToolbarDecorator.createDecorator(table!!)
                .addExtraAction(object: AnActionButton("Refresh", AllIcons.Actions.Refresh){
                    override fun actionPerformed(p0: AnActionEvent) {
                        vm.refreshProcessList()
                        updateSelection(table!!)
                    }
                })
                .createPanel()
    }

    private fun updateSelection(table: JBTable) {
        val row = viewModel!!.editorProcesses.indexOfFirst { it.pid == viewModel!!.pid.value }
        if (row > -1) table.setRowSelectionInterval(row, row)
    }
}