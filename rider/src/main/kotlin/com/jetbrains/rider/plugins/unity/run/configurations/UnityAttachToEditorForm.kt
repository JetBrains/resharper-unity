package com.jetbrains.rider.plugins.unity.run.configurations

import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus

class UnityAttachToEditorForm(viewModel: UnityAttachToEditorViewModel)
    : UnityAttachToEditorFormLayout() {

    init {
        processesList.init(viewModel)

        editorInstanceJsonError.text = when (viewModel.editorInstanceJsonStatus) {
            EditorInstanceJsonStatus.Error -> "Error reading Library/EditorInstance.json"
            EditorInstanceJsonStatus.Missing -> "Cannot read Library/EditorInstance.json - file is missing"
            EditorInstanceJsonStatus.Outdated -> "Outdated process ID from Library/EditorInstance.json"
            else -> ""
        }

        editorInstanceJsonErrorPanel.isVisible = viewModel.editorInstanceJsonStatus != EditorInstanceJsonStatus.Valid
        editorInstanceJsonInfoPanel.isVisible = viewModel.editorInstanceJsonStatus == EditorInstanceJsonStatus.Valid

        processIdInfo.text = "Using process ID ${viewModel.pid.value} from Library/EditorInstance.json"

        // EditorInstance.json always takes priority of manually choosing
        processesList.isEnabled = viewModel.editorInstanceJsonStatus != EditorInstanceJsonStatus.Valid

        // This is the sound of me giving up. Changing the selection of the JTable doesn't
        // mark the SettingsEditor as modified - whoever is interested subscribes to the
        // events from this#getPanel() and children, but excludes JTable. I've tried raising
        // SettingsEditor#fireEditorStateChanged, but that class is usually wrapped, and
        // the wrapper doesn't forward those events. The UI event subscription works, though,
        // because the wrapper is also a parent to this panel, and it subscribes to all
        // children recursively. So this is a complete cheat, and I'll stuff the value in a
        // hidden text field so that the UI events catch it. Ho hum.
        viewModel.pid.advise(viewModel.lifetime) { textField1.text = it?.toString() ?: "" }
    }
}