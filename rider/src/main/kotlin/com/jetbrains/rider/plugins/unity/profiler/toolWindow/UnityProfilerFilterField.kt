package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.ui.SearchTextField
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.intersect
import com.jetbrains.rider.plugins.unity.profiler.utils.bindTextFieldToProperty
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerTreeViewModel

class UnityProfilerFilterField(
    viewModel: UnityProfilerTreeViewModel,
    lifetime: Lifetime
) : SearchTextField(false) {
    init {
        bindTextFieldToProperty(
            textComponent = textEditor,
            property = viewModel.filterText,
            lifetime = viewModel.lifetime.intersect(lifetime),
            onTextChanged = { text -> viewModel.setFilter(text, false) }
        )
    }
}
