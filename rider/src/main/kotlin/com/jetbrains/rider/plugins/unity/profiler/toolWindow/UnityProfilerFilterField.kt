package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.ui.DocumentAdapter
import com.intellij.ui.SearchTextField
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.intersect
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerTreeViewModel
import javax.swing.event.DocumentEvent

class UnityProfilerFilterField(
    private val viewModel: UnityProfilerTreeViewModel,
    lifetime: Lifetime
) : SearchTextField(false) {
    init {
        addDocumentListener(object : DocumentAdapter() {
            override fun textChanged(e: DocumentEvent) {
                if (text != viewModel.filterText.value) {
                    viewModel.setFilter(text, false)
                }
            }
        })

        viewModel.filterText.advise(viewModel.lifetime.intersect(lifetime)) {
            if (text != it) {
                text = it
            }
        }
    }
}
