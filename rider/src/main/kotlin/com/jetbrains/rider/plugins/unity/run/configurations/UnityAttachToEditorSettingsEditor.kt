package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.impl.CheckableRunConfigurationEditor
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService

class UnityAttachToEditorSettingsEditor(project: Project) : SettingsEditor<UnityAttachToEditorRunConfiguration>(),
                                                            CheckableRunConfigurationEditor<UnityAttachToEditorRunConfiguration> {

    private val lifetimeDefinition: LifetimeDefinition = UnityProjectLifetimeService.getNestedLifetimeDefinition(project)
    private val viewModel: UnityAttachToEditorViewModel = UnityAttachToEditorViewModel(lifetimeDefinition.lifetime, project)
    private val form: UnityAttachToEditorForm = UnityAttachToEditorForm(viewModel)

    init {
        // This doesn't work, because this editor seems to be wrapped, and any listeners
        // subscribe to the wrapper, not this class, so firing this doesn't do any good.
        viewModel.pid.advise(lifetimeDefinition.lifetime) { fireEditorStateChanged() }
    }

    override fun checkEditorData(configuration: UnityAttachToEditorRunConfiguration) {
        configuration.pid = viewModel.pid.value
        configuration.useMixedMode = viewModel.useMixedMode.value
    }

    override fun resetEditorFrom(configuration: UnityAttachToEditorRunConfiguration) {
        viewModel.pid.value = configuration.pid
        viewModel.useMixedMode.value = configuration.useMixedMode
    }

    override fun applyEditorTo(configuration: UnityAttachToEditorRunConfiguration) {
        checkEditorData(configuration)
    }

    override fun createEditor() = form.panel

    override fun disposeEditor() {
        lifetimeDefinition.terminate()
    }
}