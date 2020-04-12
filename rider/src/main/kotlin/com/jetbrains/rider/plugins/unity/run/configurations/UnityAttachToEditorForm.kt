package com.jetbrains.rider.plugins.unity.run.configurations

import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus

class UnityAttachToEditorForm(viewModel: UnityAttachToEditorViewModel)
    : UnityAttachToEditorFormLayout() {

    init {
        editorInstanceJsonErrorPanel.isVisible = false
        editorInstanceJsonInfoPanel.isVisible = false

        processesList.init(viewModel)

        viewModel.editorInstanceJsonStatus.advise(viewModel.lifetime) {
            editorInstanceJsonError.text = when (it) {
                EditorInstanceJsonStatus.Error -> "Error reading Library/EditorInstance.json"
                EditorInstanceJsonStatus.Missing -> "Cannot read Library/EditorInstance.json - file is missing"
                EditorInstanceJsonStatus.Outdated -> "Outdated process ID from Library/EditorInstance.json"
                else -> ""
            }

            editorInstanceJsonErrorPanel.isVisible = it != null && it != EditorInstanceJsonStatus.Valid
            editorInstanceJsonInfoPanel.isVisible = it == EditorInstanceJsonStatus.Valid

            // EditorInstance.json always takes priority of manually choosing
            processesList.isEnabled = it != EditorInstanceJsonStatus.Valid
        }

        viewModel.pid.advise(viewModel.lifetime) {

            val value = it?.toString() ?: ""

            // This text is only shown when editorInstanceJsonInfoPanel is visible
            processIdInfo.text = "Using process ID $value from Library/EditorInstance.json"

            // This is the sound of me giving up. Changing the selection of the JTable doesn't
            // mark the SettingsEditor as modified - whoever is interested subscribes to the
            // events from this#getPanel() and children, but excludes JTable. I've tried raising
            // SettingsEditor#fireEditorStateChanged, but that class is usually wrapped, and
            // the wrapper doesn't forward those events. The UI event subscription works, though,
            // because the wrapper is also a parent to this panel, and it subscribes to all
            // children recursively. So this is a complete cheat, and I'll stuff the value in a
            // hidden text field so that the UI events catch it. Ho hum.
            textField1.text = value
        }
    }
}