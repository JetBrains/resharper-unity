package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.application.subscribe
import com.intellij.ide.CopyProvider
import com.intellij.ide.ui.UISettings
import com.intellij.ide.ui.UISettingsListener
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.DataSink
import com.intellij.openapi.actionSystem.PlatformDataKeys
import com.intellij.openapi.actionSystem.UiDataProvider
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
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.projectView.solutionDirectory
import java.awt.Font
import java.awt.datatransfer.StringSelection
import javax.swing.DefaultListModel
import javax.swing.ListSelectionModel

class UnityLogPanelEventList(lifetime: Lifetime) : JBList<LogPanelItem>(emptyList()), UiDataProvider, CopyProvider {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    val riderModel: DefaultListModel<LogPanelItem>
        get() = model as DefaultListModel<LogPanelItem>

    init {
        cellRenderer = UnityLogPanelEventRenderer()
        selectionMode = ListSelectionModel.MULTIPLE_INTERVAL_SELECTION
        emptyText.text = UnityBundle.message("log.is.empty")
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

        var result: Navigatable? = null
        if (node.stackTrace == "") {
            val regex = Regex("""(?<path>^.*(?=\())\((?<line>\d+(?=,)),(?<col>\d+(?=\)))""")
            val match = regex.find(node.message)
            var col = 0
            if (match != null) {
                col = match.groups["col"]?.value?.toInt()?.minus(1) ?: 0
            }
            result = getNavigatableForSelectedInternal(match, project, col)
        }


        if (result == null) {
            // Use first (at Assets/NewBehaviourScript.cs:16) in stacktrace
            val regex = Regex("""\(at (?<path>.*(?=:)):(?<line>\d+(?=\)))""")
            val match = regex.find(node.stackTrace)
            result = getNavigatableForSelectedInternal(match, project)
        }

        if (result == null) {
            val regex = Regex("""in (?<path>.*(?=:)):(?<line>\d+)""")
            // Use first in `Assets/NewBehaviourScript.cs:16` in message // https://fogbugz.unity3d.com/default.asp?1405031_g5l0vob5qeovfjo9
            val match = regex.find(node.message)
            result = getNavigatableForSelectedInternal(match, project)
        }

        return result
    }

    private fun getNavigatableForSelectedInternal(match: MatchResult?, project: Project, col: Int = 0): Navigatable? {
        if (match == null) return null

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

    override fun uiDataSnapshot(sink: DataSink) {
        sink[PlatformDataKeys.COPY_PROVIDER] = this
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