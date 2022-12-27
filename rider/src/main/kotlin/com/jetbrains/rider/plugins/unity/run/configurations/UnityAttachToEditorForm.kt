package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.openapi.observable.properties.AtomicProperty
import com.intellij.ui.dsl.builder.*
import com.intellij.ui.dsl.gridLayout.Gaps
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus
import javax.swing.JPanel

class UnityAttachToEditorForm(viewModel: UnityAttachToEditorViewModel){
    protected var rootPanel: JPanel? = null
    protected lateinit var commentRow: Row
    protected lateinit var editorInstanceJsonInfoRow: Row
    protected var processIdInfo = AtomicProperty("")
    protected var editorInstanceJsonError = AtomicProperty("")
    val panel: JPanel
        get() = (rootPanel)!!
    init {
        val processesList = ProcessesPanel()
        processesList.init(viewModel)

        rootPanel = panel {

            editorInstanceJsonInfoRow = row {
                label(UnityBundle.message("editorinstance.error")).bindText(editorInstanceJsonError)
            }

            indent {
                commentRow = row {
                    label("").customize(Gaps(0))
                        .comment(UnityBundle.message(
                        "comment.label.text.editorinstance.json.file.required.to.automatically.configure.run.configuration"))
                }
            }

            row {
                label(UnityBundle.message("using.process")).bindText(processIdInfo)
            }

            row {
                cell(processesList)
                    .customize(Gaps(50, 0))
                    .align(Align.FILL)
            }
        }

        viewModel.editorInstanceJsonStatus.advise(viewModel.lifetime) {
            editorInstanceJsonError.set(when (it) {
                                                EditorInstanceJsonStatus.Error -> UnityBundle.message("error.text.error.reading.library.editorinstance.json")
                                                EditorInstanceJsonStatus.Missing -> UnityBundle.message("error.text.cannot.read.library.editorinstance.json.file.is.missing")
                                                EditorInstanceJsonStatus.Outdated -> UnityBundle.message("error.text.outdated.process.id.from.library.editorinstance.json")
                                                else -> ""
                                            })

            commentRow.visible(it != null && it != EditorInstanceJsonStatus.Valid)

            // EditorInstance.json always takes priority of manually choosing
            processesList.isEnabled = it != EditorInstanceJsonStatus.Valid
        }

        viewModel.pid.advise(viewModel.lifetime) {

            val value = it?.toString()

            if (value == null)
                processIdInfo.set("")
            else
            {
                processIdInfo.set( UnityBundle.message("using.process.id.0.from.library.editorinstance.json", value))
            }
        }
    }
}