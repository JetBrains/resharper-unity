package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.application.runInEdt
import com.intellij.ui.DocumentAdapter
import com.intellij.ui.SearchTextField
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.intersect
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerTreeViewModel
import javax.swing.event.DocumentEvent

class UnityProfilerFilterField(
    viewModel: UnityProfilerTreeViewModel,
    lifetime: Lifetime
) : SearchTextField(false) {
    init {
        val bindingLifetime = viewModel.lifetime.intersect(lifetime)
        var updatingFromProperty = false

        textEditor.document.addDocumentListener(object : DocumentAdapter() {
            override fun textChanged(e: DocumentEvent) {
                if (!updatingFromProperty) {
                    viewModel.setFilterFromInput(textEditor.text)
                }
            }
        })

        viewModel.filterState.advise(bindingLifetime) { state ->
            if (textEditor.text != state.text) {
                runInEdt {
                    if (bindingLifetime.isAlive) {
                        updatingFromProperty = true
                        try {
                            textEditor.text = state.text
                        } finally {
                            updatingFromProperty = false
                        }
                    }
                }
            }
        }
    }
}
