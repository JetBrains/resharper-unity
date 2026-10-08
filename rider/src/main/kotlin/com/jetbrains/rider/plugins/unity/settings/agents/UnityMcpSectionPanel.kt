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
    fun build(presentation: UnityMcpSectionPresentation): JComponent = panel {
        presentation.project?.let { projectRow(it) }

        val gate = unityMcpGateMessage(presentation.state)
        if (gate != null) row { comment(gate) }

        rowsRange {
            storeGroup(
                presentation,
                UnityBundle.message("unity.agents.mcp.group.external"),
                UnityBundle.message("unity.agents.mcp.group.external.description"),
            )
            manualGroup(presentation.cli.location, presentation.project)
        }
            .enabled(presentation.state.canAct)
            .visible(presentation.state.drawsBody)
    }

    private fun Panel.projectRow(unityProject: UnityMcpProject) {
        row {
            label(UnityBundle.message("unity.agents.mcp.project", unityProject.name)).gap(RightGap.SMALL)
            comment(unityProject.path)
        }
    }

    private fun Panel.storeGroup(
        presentation: UnityMcpSectionPresentation,
        @Nls title: String,
        @Nls description: String,
    ) {
        if (presentation.rows.isEmpty()) return
        group(title) {
            row { comment(description) }
            presentation.rows.forEach { storeRow(it, presentation.cli) }
        }
    }

    private fun Panel.storeRow(row: UnityMcpRowPresentation, cli: UnityCliPresentation) {
        row { label(unityMcpStoreName(row.store)).bold() }

        val status = unityMcpRowStatus(row)
        row {
            cell(JBOptionButton(configureAction(row), dropdownActions(row, cli))).gap(RightGap.SMALL)
            status.icon?.let { icon(it).gap(RightGap.SMALL) }
            if (status.isProblem) comment(status.text) else label(status.text)
        }

        unityMcpRowDetail(row)?.let { detail -> row { comment(detail) } }
    }

    private fun configureAction(row: UnityMcpRowPresentation): Action {
        val label =
            if (row.state == UnityMcpRowState.FAILED) UnityBundle.message("unity.agents.mcp.action.try.again")
            else UnityBundle.message("unity.agents.mcp.action.configure")
        return action(label, enabled = row.state != UnityMcpRowState.BUSY) {
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

    private fun Panel.manualGroup(unity: Path?, unityProject: UnityMcpProject?) {
        group(UnityBundle.message("unity.agents.mcp.manual.title")) {
            row { comment(UnityBundle.message("unity.agents.mcp.manual.description")) }
            row {
                button(UnityBundle.message("unity.agents.mcp.action.copy.stdio")) {
                    unity?.let { copy(unityMcpManualSnippet(it, unityProject?.let { p -> Path.of(p.path) })) }
                }.enabled(unity != null)
            }
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
