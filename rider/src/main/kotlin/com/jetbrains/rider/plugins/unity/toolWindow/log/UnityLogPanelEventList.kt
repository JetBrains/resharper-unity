package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.application.subscribe
import com.intellij.ide.CopyProvider
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.DataProvider
import com.intellij.openapi.actionSystem.PlatformDataKeys
import com.intellij.openapi.editor.colors.EditorColorsListener
import com.intellij.openapi.editor.colors.EditorColorsManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.ide.CopyPasteManager
import com.intellij.openapi.project.Project
import com.intellij.pom.Navigatable
import com.intellij.ui.TreeUIHelper
import com.intellij.ui.components.JBList
import com.jetbrains.rdclient.util.idea.createNestedDisposable
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.util.lifetime.Lifetime
import java.awt.Font
import java.awt.datatransfer.StringSelection
import java.io.File
import javax.swing.DefaultListModel
import javax.swing.ListSelectionModel

class UnityLogPanelEventList(lifetime: Lifetime) : JBList<LogPanelItem>(emptyList()), DataProvider, CopyProvider {
    val riderModel: DefaultListModel<LogPanelItem>
        get() = model as DefaultListModel<LogPanelItem>

    init {
        cellRenderer = UnityLogPanelEventRenderer()
        selectionMode = ListSelectionModel.MULTIPLE_INTERVAL_SELECTION
        emptyText.text = "Log is empty"
        TreeUIHelper.getInstance().installListSpeedSearch(this)

        EditorColorsManager.TOPIC.subscribe(lifetime.createNestedDisposable(), EditorColorsListener { updateFont() })
        updateFont()
    }

    private fun updateFont() {
        val sc = EditorColorsManager.getInstance().globalScheme
        font = Font(sc.consoleFontName, Font.PLAIN, sc.consoleFontSize)
    }

    fun getNavigatableForSelected(list: UnityLogPanelEventList, project: Project): Navigatable? {
        val node = list.selectedValue
        if (node!=null && (node.stackTrace=="")) {
            val index = node.message.indexOf("(")
            if (index<0)
                return null
            val path = node.message.substring(0, index)
            val regex = Regex("^\\(\\d{1,}\\,\\d{1,}\\)")
            val res = regex.find(node.message.substring(index), 0) ?: return null
            val coordinates = res.value.substring(1, res.value.length-1).split(",")
            val line = (coordinates[0])
            val col = coordinates[1]

            val file = File(project.basePath, path)
            if (!file.exists())
                return null
            val virtualFile = file.toVirtualFile()
            return if (virtualFile != null)
                OpenFileDescriptor(project, virtualFile , line.toInt()-1, col.toInt()-1, true)
            else
                null
        }
        return null
    }

    override fun getData(dataId: String): Any? = when {
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

    private fun LogPanelItem.getStackTraceForCopy() = stackTrace
        .split('\r', '\n')
        .filter { it.isNotEmpty() }
        .joinToString("\n") { "    $it" }

    private fun LogPanelItem.getTextForCopy(): String {
        val header = "[$type $mode] $message"
        return if (stackTrace.trim().isEmpty()) header else header + "\n" + getStackTraceForCopy()
    }
}