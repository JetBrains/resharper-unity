package com.jetbrains.rider.plugins.unity.run.configurations.devices

import com.intellij.execution.CantRunException
import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.internal.statistic.eventLog.events.EventPair
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.ui.dsl.builder.AlignX
import com.intellij.ui.dsl.builder.panel
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorRunConfiguration
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

class UnityDevicePlayerConfiguration(project: Project, factory: UnityDevicePlayerFactory) :
    UnityRunConfigurationBase(project, factory),
    DevicesConfiguration {


    private val editorConfigurationType = ConfigurationTypeUtil.findConfigurationType(UnityEditorDebugConfigurationType::class.java)
    private val editorConfiguration = UnityAttachToEditorRunConfiguration(project, editorConfigurationType.attachToEditorAndPlayFactory, false)
    private val editorAndPlayConfiguration = UnityAttachToEditorRunConfiguration(project, editorConfigurationType.attachToEditorAndPlayFactory, true)

    override suspend fun getRunProfileStateAsync(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        val manager = ActiveDeviceManager.getInstance(project)
        val process = manager.getDevice<UnityProcess>()

        if (process != null) {
            if (process is UnityEditorEntryPoint)
                return editorConfiguration.getState(executor, environment) ?: throw CantRunException(
                    UnityBundle.message("dialog.message.failed.to.use.attach.to.unity.editor.run.configuration"))
            if (process is UnityEditorEntryPointAndPlay)
                return editorAndPlayConfiguration.getState(executor, environment) ?: throw CantRunException(
                    UnityBundle.message("dialog.message.failed.to.use.attach.to.unity.editor.run.configuration"))
        }

        if (process == null) { throw CantRunException(UnityBundle.message("failed.to.identify.device")) }
        populateStateFromProcess(state, process)

        return getRunProfileStateAsyncInternal(executor, environment)
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration?> {
        val manager = ActiveDeviceManager.getInstance(project)
        val process = manager.getDevice<UnityProcess>()
        if (process != null) {
            if (
                process is UnityEditorEntryPoint)
                return editorConfiguration.configurationEditor
            if (process is UnityEditorEntryPointAndPlay)
                return editorAndPlayConfiguration.configurationEditor
        }

        return object : SettingsEditor<UnityDevicePlayerConfiguration>() {
            override fun resetEditorFrom(config: UnityDevicePlayerConfiguration) {
            }

            private val panel = panel {
                row {
                    label(manager.activeDeviceView.value?.name ?:"").align(AlignX.FILL)
                }
            }

            override fun applyEditorTo(s: UnityDevicePlayerConfiguration) {
                //nothing editable
            }

            override fun createEditor(): JComponent {
                return panel
            }
        }
    }

    override val provider: DevicesProvider? = UnityDevicesProvider.getService(project)

    override fun getAdditionalUsageData(): List<EventPair<*>> {
        return ActiveDeviceManager.getInstance(project).getStatisticsDeviceData()
    }
}

class UnityDevicePlayerFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {
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