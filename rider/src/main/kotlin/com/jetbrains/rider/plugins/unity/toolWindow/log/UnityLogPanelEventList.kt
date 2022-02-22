package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.application.subscribe
import com.intellij.ide.CopyProvider
import com.intellij.ide.ui.UISettings
import com.intellij.ide.ui.UISettingsListener
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.DataProvider
import com.intellij.openapi.actionSystem.PlatformDataKeys
import com.intellij.openapi.editor.colors.EditorColorsListener
import com.intellij.openapi.editor.colors.EditorColorsManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.ide.CopyPasteManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.pom.Navigatable
import com.intellij.ui.TreeUIHelper
import com.intellij.ui.components.JBList
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.projectView.solutionDirectory
import java.awt.Font
import java.awt.datatransfer.StringSelection
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

        val disposable = lifetime.createNestedDisposable()
        EditorColorsManager.TOPIC.subscribe(disposable, EditorColorsListener { updateFont() })
        UISettingsListener.TOPIC.subscribe(disposable, UISettingsListener { updateFont() })

        updateFont()
    }

    private fun updateFont() {
        val sc = EditorColorsManager.getInstance().globalScheme
        font = if (UISettings.getInstance().presentationMode) {
            Font(sc.consoleFontName, Font.PLAIN, UISettings.getInstance().presentationModeFontSize)
        }
        else {
            Font(sc.consoleFontName, Font.PLAIN, sc.consoleFontSize)
        }
        emptyText.setFont(UIUtil.getLabelFont())
    }

    fun getNavigatableForSelected(project: Project): Navigatable? {
        val node = selectedValue ?: return null

        val match: MatchResult?
        var col = 0
        if (node.stackTrace=="") {
            val regex = Regex("""(?<path>^.*(?=\())\((?<line>\d+(?=,)),(?<col>\d+(?=\)))""")
            match = regex.find(node.message) ?: return null
            col =  (match.groups["col"]?.value?.toInt() ?: return null)-1
        }
        else {
            // Use first (at Assets/NewBehaviourScript.cs:16) in stacktrace
            val regex = Regex("""\(at (?<path>.*(?=:)):(?<line>\d+(?=\)))""")
            match = regex.find(node.stackTrace) ?: return null
        }

        val path = match.groups["path"]?.value ?: return null
        val line = (match.groups["line"]?.value?.toInt() ?: return null) - 1

        val file = project.solutionDirectory.resolve(path)
        if (!file.exists())
            return null
        val virtualFile = file.toVirtualFile()
        return if (virtualFile != null)
            OpenFileDescriptor(project, virtualFile, line, col, true)
        else
            null
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