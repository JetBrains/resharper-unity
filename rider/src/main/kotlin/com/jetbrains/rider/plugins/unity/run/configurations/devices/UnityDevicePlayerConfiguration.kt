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
import com.jetbrains.rider.plugins.unity.run.UnityEditorEntryPoint
import com.jetbrains.rider.plugins.unity.run.UnityEditorEntryPointAndPlay
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorSettingsEditor
import com.jetbrains.rider.plugins.unity.run.configurations.UnityConfigurationFactoryBase
import com.jetbrains.rider.plugins.unity.run.configurations.UnityEditorDebugConfigurationType
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfigurationOptions
import com.jetbrains.rider.plugins.unity.run.configurations.UnityRunConfigurationBase
import com.jetbrains.rider.plugins.unity.run.configurations.populateStateFromProcess
import com.jetbrains.rider.plugins.unity.run.devices.UnityDevicesProvider
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

    private val activeRunConfiguration: UnityAttachToEditorRunConfiguration?
        get() =
            when (ActiveDeviceManager.getInstance(this.project).getDevice<UnityProcess>()) {
                is UnityEditorEntryPoint -> editorConfiguration
                is UnityEditorEntryPointAndPlay -> editorAndPlayConfiguration
                else -> null
            }

    override suspend fun getRunProfileStateAsync(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        val manager = ActiveDeviceManager.getInstance(project)
        val process = manager.getDevice<UnityProcess>()

        activeRunConfiguration?.let { activeConfiguration ->
            return activeConfiguration.getState(executor, environment) ?: throw CantRunException(
                UnityBundle.message("dialog.message.failed.to.use.attach.to.unity.editor.run.configuration"))
        }

        if (process == null) { throw CantRunException(UnityBundle.message("failed.to.identify.device")) }
        populateStateFromProcess(state, process)

        return getRunProfileStateAsyncInternal(executor, environment)
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration?> {
        val manager = ActiveDeviceManager.getInstance(project)

        // This method is called when user opens a dialog to edit the run configuration, put the current active run configuration in a variable
        // as the changes the user makes should be applied exactly to it
        val activeRunConfiguration = activeRunConfiguration
        val editor = activeRunConfiguration?.configurationEditor as? UnityAttachToEditorSettingsEditor?

        // We need the generic T of SettingsEditor<T> to be UnityDevicePlayerConfiguration, so return our own instance but delegate inside
        return object : SettingsEditor<UnityDevicePlayerConfiguration>(), CheckableRunConfigurationEditor<UnityDevicePlayerConfiguration> {
            private val panel = lazy {
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

            override fun createEditor(): JComponent = editor?.createEditor() ?: panel.value

            override fun disposeEditor() {
                editor?.disposeEditor()
            }

            override fun checkEditorData(s: UnityDevicePlayerConfiguration?) {
                editor?.checkEditorData(activeRunConfiguration)
            }
        }
    }

    override val provider: DevicesProvider? = UnityDevicesProvider.getService(project)

    override fun getAdditionalUsageData(): List<EventPair<*>> {
        return ActiveDeviceManager.getInstance(project).getStatisticsDeviceData()
    }

    override fun useMixedDebugMode(): Boolean = activeRunConfiguration?.useMixedDebugMode() ?: false
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