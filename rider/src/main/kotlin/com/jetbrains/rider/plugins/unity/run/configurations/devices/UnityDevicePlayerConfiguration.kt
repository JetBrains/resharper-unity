package com.jetbrains.rider.plugins.unity.run.configurations.devices

import com.intellij.execution.CantRunException
import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.ui.dsl.builder.AlignX
import com.intellij.ui.dsl.builder.panel
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.UnityEditor
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachProfileState
import com.jetbrains.rider.plugins.unity.run.configurations.UnityConfigurationFactoryBase
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfigurationOptions
import com.jetbrains.rider.plugins.unity.run.devices.UnityDevice
import com.jetbrains.rider.plugins.unity.run.devices.UnityDevicesProvider
import com.jetbrains.rider.run.RiderRunBundle
import com.jetbrains.rider.run.configurations.AsyncRunConfiguration
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.devices.ActiveDeviceManager
import com.jetbrains.rider.run.devices.DevicesConfiguration
import com.jetbrains.rider.run.devices.DevicesProvider
import icons.UnityIcons
import org.jetbrains.concurrency.Promise
import javax.swing.JComponent

// single config for all players, which utilize DevicesConfiguration
class UnityDevicePlayerConfiguration(project: Project, factory: UnityDevicePlayerFactory) :
    RunConfigurationBase<UnityPlayerDebugConfigurationOptions>(project, factory, null),
    RunConfigurationWithSuppressedDefaultRunAction,
    AsyncRunConfiguration,
    WithoutOwnBeforeRunSteps,
    IRiderDebuggable,
    DevicesConfiguration {

    override fun getState(): UnityPlayerDebugConfigurationOptions = options as UnityPlayerDebugConfigurationOptions

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration?> {
        return object : SettingsEditor<UnityDevicePlayerConfiguration>() {
            override fun resetEditorFrom(config: UnityDevicePlayerConfiguration) {
            }

            private val panel = panel {
                row {
                    label(ActiveDeviceManager.getInstance(project).activeDeviceView.value?.name ?:"").align(AlignX.FILL)
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

    @Suppress("UsagesOfObsoleteApi")
    @Deprecated("Please, override 'getRunProfileStateAsync' instead")
    override fun getStateAsync(executor: Executor, environment: ExecutionEnvironment): Promise<RunProfileState> {
        @Suppress("DEPRECATION")
        throw UnsupportedOperationException(
            RiderRunBundle.message("obsolete.synchronous.api.is.used.message", UnityPlayerDebugConfiguration::getStateAsync.name))
    }

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? =
        throw UnsupportedOperationException("Synchronous call to getState is not supported")

    override suspend fun getRunProfileStateAsync(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID) {
            @Suppress("HardCodedStringLiteral")
            throw CantRunException("Unexpected executor ID: ${executor.id}")
            // TODO: We should be able to return resolvedPromise(null), but the function's type doesn't allow this
        }
        val device = ActiveDeviceManager.getInstance(project).getDevice<UnityDevice>()
        return UnityAttachProfileState(getRemoteConfiguration(), environment, name, device?.process is UnityEditor)
    }

    private fun getRemoteConfiguration() = object : RemoteConfiguration {
        val activeDevice = ActiveDeviceManager.getInstance(project).getDevice<UnityDevice>()
        override var address = activeDevice?.process?.host ?: state.host!!
        override var port = activeDevice?.process?.port ?: state.port
        override var listenPortForConnections = false
    }

    override val provider: DevicesProvider? = UnityDevicesProvider.getService(project)
}

class UnityDevicePlayerFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {
    override fun createTemplateConfiguration(project: Project): UnityDevicePlayerConfiguration =
        UnityDevicePlayerConfiguration(project, this)

    override fun getId(): String = "UnityAttachToDevicePlayer"
    override fun getOptionsClass(): Class<UnityPlayerDebugConfigurationOptions> = UnityPlayerDebugConfigurationOptions::class.java
}

internal class UnityDevicePlayerDebugConfigurationType : ConfigurationTypeBase(
    ID,
    UnityBundle.message("configuration.type.name.attach.to.unity.device.player"),
    UnityBundle.message("configuration.type.description.attach.to.unity.device.player.and.debug"),
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