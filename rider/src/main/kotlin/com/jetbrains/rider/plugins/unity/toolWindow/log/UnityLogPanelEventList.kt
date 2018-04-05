package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ide.CopyProvider
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.DataProvider
import com.intellij.openapi.actionSystem.PlatformDataKeys
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.ide.CopyPasteManager
import com.intellij.openapi.project.Project
import com.intellij.pom.Navigatable
import com.intellij.ui.TreeUIHelper
import com.intellij.ui.components.JBList
import com.jetbrains.rider.plugins.unity.editorPlugin.model.*
import com.jetbrains.rider.util.idea.toVirtualFile
import java.awt.datatransfer.StringSelection
import java.io.File
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

    fun getNavigatableForSelected(list: UnityLogPanelEventList, project: Project): Navigatable? {
        val node = list.selectedValue
        if (node!=null && (node.stackTrace=="")) {
            var index = node.message.indexOf("(");
            if (index<0)
                return null
            var path = node.message.substring(0, index)
            var regex = Regex("^\\(\\d{1,}\\,\\d{1,}\\)")
            var res = regex.find(node.message.substring(index), 0)
            if (res==null)
                return null
            var coordinates = res.value.substring(1, res.value.length-1).split(",")
            var line = (coordinates[0])
            var col = coordinates[1]

            val file = File(project.baseDir.path, path)
            if (!file.exists())
                return null
            var virtualFile = file.toVirtualFile()
            if (virtualFile != null)
                return OpenFileDescriptor(project, virtualFile , line.toInt()-1, col.toInt()-1, true)
            else
                return null
        }
        return null;
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