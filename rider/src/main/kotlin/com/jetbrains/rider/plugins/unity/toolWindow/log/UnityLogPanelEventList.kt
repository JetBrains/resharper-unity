package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ide.CopyProvider
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.ide.CopyPasteManager
import com.intellij.ui.TreeUIHelper
import com.intellij.ui.components.JBList
import com.jetbrains.rider.plugins.unity.RdLogEvent
import java.awt.datatransfer.StringSelection
import javax.swing.DefaultListModel
import javax.swing.ListSelectionModel

class UnityLogPanelEventList : JBList<RdLogEvent>(emptyList()), DataProvider, CopyProvider {
    val riderModel: DefaultListModel<RdLogEvent>
        get() = model as DefaultListModel<RdLogEvent>

    init {
        cellRenderer = UnityLogPanelEventRenderer()
        selectionMode = ListSelectionModel.MULTIPLE_INTERVAL_SELECTION
        emptyText.text = "Log is empty"
        TreeUIHelper.getInstance().installListSpeedSearch(this)
    }

    override fun getData(dataId: String?): Any? = when {
        PlatformDataKeys.COPY_PROVIDER.`is`(dataId) -> this
        else -> null
    }

    override fun performCopy(dataContext: DataContext) {
        if (!isSelectionEmpty) {
            CopyPasteManager.getInstance().setContents(StringSelection(getTextForCopy()))
        }
    }

    private fun getTextForCopy() = selectedValuesList.joinToString("\n") { it.getTextForCopy() }

    override fun isCopyEnabled(dataContext: DataContext) = !isSelectionEmpty
    override fun isCopyVisible(dataContext: DataContext) = !isSelectionEmpty

    private fun RdLogEvent.getStackTraceForCopy() = stackTrace
        .split('\r', '\n')
        .filter { it.isNotEmpty() }
        .joinToString("\n") { "    " + it }

    private fun RdLogEvent.getTextForCopy(): String {
        val header = "[$type $mode] $message"
        return if (stackTrace.isBlank()) header else header + "\n" + getStackTraceForCopy()
    }
}