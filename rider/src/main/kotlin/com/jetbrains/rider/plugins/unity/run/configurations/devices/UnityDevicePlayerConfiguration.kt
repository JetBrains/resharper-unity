package com.jetbrains.rider.plugins.unity.run.configurations.devices

import com.intellij.execution.CantRunException
import com.intellij.execution.Executor
import com.intellij.execution.configurations.ConfigurationType
import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.configurations.RunConfigurationSingletonPolicy
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.configurations.VirtualConfigurationType
import com.intellij.execution.impl.CheckableRunConfigurationEditor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.internal.statistic.eventLog.events.EventPair
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.ui.dsl.builder.AlignX
import com.intellij.ui.dsl.builder.panel
import com.jetbrains.rider.debugger.IMixedModeDebugAwareRunProfile
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorSettingsEditor
import com.jetbrains.rider.plugins.unity.run.configurations.UnityConfigurationFactoryBase
import com.jetbrains.rider.plugins.unity.run.configurations.UnityEditorDebugConfigurationType
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfigurationOptions
import com.jetbrains.rider.plugins.unity.run.configurations.UnityRunConfigurationBase
import com.jetbrains.rider.plugins.unity.run.configurations.populateStateFromProcess
import com.jetbrains.rider.plugins.unity.run.devices.UnityCurrentProjectEditorDevice
import com.jetbrains.rider.plugins.unity.run.devices.UnityDevicesProvider
import com.jetbrains.rider.plugins.unity.run.devices.UnityEditorDeviceKind
import com.jetbrains.rider.plugins.unity.run.devices.UnityProcessDevice
import com.jetbrains.rider.run.devices.ActiveDeviceManager
import com.jetbrains.rider.run.devices.DevicesConfiguration
import com.jetbrains.rider.run.devices.DevicesProvider
import icons.UnityIcons
import javax.swing.JComponent

internal class UnityDevicePlayerConfiguration(project: Project, factory: UnityDevicePlayerFactory) :
    UnityRunConfigurationBase(project, factory),
    DevicesConfiguration,
    IMixedModeDebugAwareRunProfile {

    private val editorConfigurationType = ConfigurationTypeUtil.findConfigurationType(UnityEditorDebugConfigurationType::class.java)
    private val editorConfiguration = UnityAttachToEditorRunConfiguration(project, editorConfigurationType.attachToEditorAndPlayFactory, false)
    private val editorAndPlayConfiguration = UnityAttachToEditorRunConfiguration(project, editorConfigurationType.attachToEditorAndPlayFactory, true)

    private fun getRunConfigurationForCurrentlySelectedEditorDevice(): UnityAttachToEditorRunConfiguration? {
        val device = ActiveDeviceManager.getInstance(this.project).getDevice<UnityCurrentProjectEditorDevice>() ?: return null
        if (device.kind == UnityEditorDeviceKind) {
            return if (device.playOnConnect) editorAndPlayConfiguration else editorConfiguration
        }
        return null
    }

    override suspend fun getRunProfileStateAsync(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        val manager = ActiveDeviceManager.getInstance(project)

        // If the currently selected device is an editor, get the well-known configuration that will attach to the current editor
        getRunConfigurationForCurrentlySelectedEditorDevice()?.let { activeConfiguration ->
            return activeConfiguration.getState(executor, environment)
                ?: throw CantRunException(UnityBundle.message("dialog.message.failed.to.use.attach.to.unity.editor.run.configuration"))
        }

        // The current device isn't an editor, update the current run configuration state, and use it to create a run profile
        // TODO: Do we need to update state here? I think this gets saved to workspace.xml, but should it be?
        // Could we just create a run profile directly from the UnityProcess?
        val device = manager.getDevice<UnityProcessDevice>()
            ?: throw CantRunException(UnityBundle.message("failed.to.identify.device"))
        populateStateFromProcess(state, device.process)
        return getRunProfileStateAsyncInternal(executor, environment)
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration?> {
        val manager = ActiveDeviceManager.getInstance(project)

        // This method is called when user opens a dialog to edit the run configuration, put the current active run configuration in a variable
        // as the changes the user makes should be applied exactly to it
        val activeRunConfiguration = getRunConfigurationForCurrentlySelectedEditorDevice()
        val editor = activeRunConfiguration?.configurationEditor as? UnityAttachToEditorSettingsEditor?

        // We need the generic T of SettingsEditor<T> to be UnityDevicePlayerConfiguration, so return our own instance but delegate inside
        return object : SettingsEditor<UnityDevicePlayerConfiguration>(), CheckableRunConfigurationEditor<UnityDevicePlayerConfiguration> {
            private val panel by lazy {
                panel {
                    row {
                        label(manager.activeDeviceView.value?.name ?: "").align(AlignX.FILL)
                    }
                }
            }

            override fun resetEditorFrom(config: UnityDevicePlayerConfiguration) {
                editor?.resetEditorFrom(activeRunConfiguration)
            }

            override fun applyEditorTo(s: UnityDevicePlayerConfiguration) {
                editor?.applyEditorTo(activeRunConfiguration)
            }

            override fun createEditor(): JComponent = editor?.createEditor() ?: panel

            override fun disposeEditor() {
                editor?.disposeEditor()
            }

            override fun checkEditorData(s: UnityDevicePlayerConfiguration?) {
                editor?.checkEditorData(activeRunConfiguration)
            }
        }
    }

    override val provider: DevicesProvider = UnityDevicesProvider.getService(project)

    override fun getAdditionalUsageData(): List<EventPair<*>> {
        return ActiveDeviceManager.getInstance(project).getStatisticsDeviceData()
    }

    override fun useMixedDebugMode(): Boolean = getRunConfigurationForCurrentlySelectedEditorDevice()?.useMixedDebugMode() ?: false
}

internal class UnityDevicePlayerFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {
    override fun createTemplateConfiguration(project: Project): UnityDevicePlayerConfiguration =
        UnityDevicePlayerConfiguration(project, this)

    override fun getId(): String = "UnityAttachToDevicePlayer"
    override fun getOptionsClass(): Class<UnityPlayerDebugConfigurationOptions> = UnityPlayerDebugConfigurationOptions::class.java
    override fun getSingletonPolicy(): RunConfigurationSingletonPolicy = RunConfigurationSingletonPolicy.MULTIPLE_INSTANCE
}

internal class UnityDevicePlayerDebugConfigurationType : ConfigurationTypeBase(
    ID,
    UnityBundle.message("configuration.type.name.attach.to.unity.device.player"),
    "",
    UnityIcons.RunConfigurations.AttachToPlayer
), VirtualConfigurationType {

    val factory = UnityDevicePlayerFactory(this)

    init {
        addFactory(factory)
    }

    companion object {
        const val ID = "UnityDevicePlayer"
    }
}
