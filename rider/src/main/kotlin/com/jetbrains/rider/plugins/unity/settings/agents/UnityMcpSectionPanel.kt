package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.ide.CopyPasteManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.refreshAndFindVirtualFile
import com.intellij.ui.components.JBOptionButton
import com.intellij.ui.dsl.builder.Panel
import com.intellij.ui.dsl.builder.RightGap
import com.intellij.ui.dsl.builder.panel
import com.jetbrains.rider.plugins.unity.UnityBundle
import org.jetbrains.annotations.Nls
import java.awt.event.ActionEvent
import java.nio.file.Path
import javax.swing.AbstractAction
import javax.swing.Action
import javax.swing.JComponent

internal class UnityMcpSectionPanel(
    private val project: Project,
    private val write: (List<UnityMcpStore>, UnityMcpOperation) -> Unit,
) {
    fun build(presentation: UnityMcpSectionPresentation, packageMissing: Boolean): JComponent = panel {
        val gate = unityMcpGateMessage(presentation.state, packageMissing)
        if (gate != null) row { comment(gate) }

        rowsRange {
            presentation.rows.forEach { storeRow(it, presentation.cli, packageMissing) }
            manualRows(presentation.cli.location, presentation.project, packageMissing)
        }
            .enabled(presentation.state.canAct)
            .visible(presentation.state.drawsBody)
    }

    private fun Panel.storeRow(row: UnityMcpRowPresentation, cli: UnityCliPresentation, packageMissing: Boolean) {
        row { label(unityMcpStoreName(row.store)) }

        val status = unityMcpRowStatus(row)
        row {
            cell(JBOptionButton(configureAction(row, packageMissing), dropdownActions(row, cli))).gap(RightGap.SMALL)
            status.icon?.let { icon(it).gap(RightGap.SMALL) }
            if (status.isProblem) comment(status.text) else label(status.text)
        }

        unityMcpRowDetail(row)?.let { detail -> row { comment(detail) } }
    }

    private fun configureAction(row: UnityMcpRowPresentation, packageMissing: Boolean): Action {
        val label =
            if (row.state == UnityMcpRowState.FAILED) UnityBundle.message("unity.agents.mcp.action.try.again")
            else UnityBundle.message("unity.agents.mcp.action.configure")
        return action(label, enabled = unityMcpCanConfigure(row) && !packageMissing) {
            write(listOf(row.store), UnityMcpOperation.CONFIGURING)
        }
    }

    private fun dropdownActions(row: UnityMcpRowPresentation, cli: UnityCliPresentation): Array<Action> {
        val actions = mutableListOf<Action>()
        unityMcpOpenableStore(row)?.let { path ->
            actions.add(action(UnityBundle.message("unity.agents.mcp.action.open.file")) { openStore(path) })
        }
        cli.location?.let { unity ->
            val snippet = unityMcpRowSnippet(row.store, unity)
            actions.add(action(UnityBundle.message("unity.agents.mcp.action.copy.entry")) { copy(snippet) })
        }
        actions.add(
            action(
                UnityBundle.message("unity.agents.mcp.action.remove"),
                enabled = unityMcpCanRemove(row),
            ) { write(listOf(row.store), UnityMcpOperation.REMOVING) }
        )
        return actions.toTypedArray()
    }

    private fun Panel.manualRows(unity: Path?, unityProject: UnityMcpProject?, packageMissing: Boolean) {
        row { label(UnityBundle.message("unity.agents.mcp.manual.title")) }
        row {
            button(UnityBundle.message("unity.agents.mcp.action.copy.stdio")) {
                unity?.let { copy(unityMcpManualSnippet(it, unityProject?.let { p -> Path.of(p.path) })) }
            }.enabled(unity != null && !packageMissing)
        }
    }

    private fun openStore(path: String) {
        val file = Path.of(path).refreshAndFindVirtualFile() ?: return
        OpenFileDescriptor(project, file).navigate(true)
    }

    private fun copy(text: String) {
        CopyPasteManager.copyTextToClipboard(text)
    }

    private fun action(@Nls text: String, enabled: Boolean = true, run: () -> Unit): Action =
        object : AbstractAction(text) {
            init {
                isEnabled = enabled
            }

            override fun actionPerformed(e: ActionEvent?) = run()
        }
}
