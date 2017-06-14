package com.jetbrains.resharper.plugins.unity.run.configurations

import com.intellij.execution.impl.CheckableRunConfigurationEditor
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.jetbrains.rider.util.lifetime.Lifetime
import com.jetbrains.rider.util.lifetime.LifetimeDefinition

class UnityAttachToEditorSettingsEditor(project: Project) : SettingsEditor<UnityAttachToEditorConfiguration>(),
        CheckableRunConfigurationEditor<UnityAttachToEditorConfiguration> {

    private val lifetimeDefinition: LifetimeDefinition = Lifetime.create(Lifetime.Eternal)
    private val viewModel: UnityAttachToEditorViewModel
    private val form: UnityAttachToEditorForm

    init {
        viewModel = UnityAttachToEditorViewModel(lifetimeDefinition.lifetime, project)
        form = UnityAttachToEditorForm(viewModel)

        // This doesn't work, because this editor seems to be wrapped, and any listeners
        // subscribe to the wrapper, not this class, so firing this doesn't do any good.
        viewModel.pid.advise(lifetimeDefinition.lifetime, { fireEditorStateChanged() })
    }

    override fun checkEditorData(configuration: UnityAttachToEditorConfiguration) {
        configuration.pid = viewModel.pid.value
    }

    override fun resetEditorFrom(configuration: UnityAttachToEditorConfiguration) {
        viewModel.pid.value = configuration.pid
    }

    override fun applyEditorTo(configuration: UnityAttachToEditorConfiguration) {
        checkEditorData(configuration)
    }

    override fun createEditor() = form.panel

    override fun disposeEditor() {
        lifetimeDefinition.terminate()
        super.disposeEditor()
    }
}