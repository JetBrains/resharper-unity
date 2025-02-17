package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.openapi.observable.properties.AtomicProperty
import com.intellij.ui.dsl.builder.Align
import com.intellij.ui.dsl.builder.Row
import com.intellij.ui.dsl.builder.bindText
import com.intellij.ui.dsl.builder.panel
import com.intellij.ui.dsl.builder.selected
import com.intellij.ui.dsl.gridLayout.UnscaledGaps
import com.jetbrains.rider.debugger.mixed.mode.isMixedModeDebugFeatureEnabled
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus
import com.jetbrains.rider.run.RiderRunBundle
import javax.swing.JPanel

internal class UnityAttachToEditorForm(viewModel: UnityAttachToEditorViewModel) {
    private var rootPanel: JPanel? = null
    private lateinit var commentRow: Row
    private lateinit var usingProcessRow: Row
    private lateinit var editorInstanceJsonInfoRow: Row
    private var processIdInfo = AtomicProperty("")
    private var editorInstanceJsonError = AtomicProperty("")

    val panel: JPanel
        get() = rootPanel!!

    init {
        val processesList = ProcessesPanel()
        processesList.init(viewModel)

        rootPanel = panel {
            editorInstanceJsonInfoRow = row {
                label(UnityBundle.message("editorinstance.error")).bindText(editorInstanceJsonError)
            }

            indent {
                commentRow = row {
                    label("").customize(UnscaledGaps.Companion.EMPTY)
                        .comment(UnityBundle.message(
                            "comment.label.text.editorinstance.json.file.required.to.automatically.configure.run.configuration"))
                }
            }

            usingProcessRow = row {
                label(UnityBundle.message("using.process")).bindText(processIdInfo)
            }

            if (isMixedModeDebugFeatureEnabled())
                row {
                    checkBox(RiderRunBundle.message("rider.use.mixed.mode.debug"))
                        .onChanged { viewModel.useMixedMode.value = it.isSelected }
                        .also { checkBox ->
                            viewModel.useMixedMode.advise(viewModel.lifetime) {
                                checkBox.selected(it)
                            }
                        }
                }

            row {
                cell(processesList)
                    .customize(UnscaledGaps(50, 0))
                    .align(Align.FILL)
            }
        }

        viewModel.editorInstanceJsonStatus.advise(viewModel.lifetime) {
            editorInstanceJsonError.set(when (it) {
                                            EditorInstanceJsonStatus.Error -> UnityBundle.message(
                                                "error.text.error.reading.library.editorinstance.json")
                                            EditorInstanceJsonStatus.Missing -> UnityBundle.message(
                                                "error.text.cannot.read.library.editorinstance.json.file.is.missing")
                                            EditorInstanceJsonStatus.Outdated -> UnityBundle.message(
                                                "error.text.outdated.process.id.from.library.editorinstance.json")
                                            else -> ""
                                        })

            commentRow.visible(it != EditorInstanceJsonStatus.Valid)
            usingProcessRow.visible(it == EditorInstanceJsonStatus.Valid)

            // EditorInstance.json always takes priority of manually choosing
            processesList.isEnabled = it != EditorInstanceJsonStatus.Valid
        }

        viewModel.pid.advise(viewModel.lifetime) {
            val value = it?.toString()
            if (value == null)
                processIdInfo.set("")
            else {
                processIdInfo.set(UnityBundle.message("using.process.id.0.from.library.editorinstance.json", value))
            }
        }
    }
}