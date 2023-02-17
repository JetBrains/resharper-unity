package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteForm
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import icons.UnityIcons
import javax.swing.JComponent

class UnityPlayerDebugConfigurationType : ConfigurationTypeBase(
    id,
    UnityBundle.message("configuration.type.name.attach.to.unity.player"),
    UnityBundle.message("configuration.type.description.attach.to.unity.player.and.debug"),
    UnityIcons.RunConfigurations.AttachToPlayer
), VirtualConfigurationType {

    val attachToPlayerFactory = UnityAttachToPlayerFactory(this)

    init {
        addFactory(attachToPlayerFactory)
    }

    companion object {
        const val id = "UnityPlayer"
    }
}

class UnityAttachToPlayerFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {
    override fun createTemplateConfiguration(project: Project) =
        UnityPlayerDebugConfiguration(project, this)

    override fun getId() = "UnityAttachToPlayer"
    override fun getOptionsClass() = UnityPlayerDebugConfigurationOptions::class.java
}

class UnityPlayerDebugConfigurationOptions: RunConfigurationOptions() {

    /**
     * A (hopefully) unique string to identify a player
     *
     * Can be used as part of heuristics to refresh the player's debugger host and port values.
     *
     * For players discovered via multicast, this is the contents of the `Id` block, e.g.
     * `iPhonePlayer(Matts-iPhone-7)` or `OSXPlayer(Matts-MacBook-Pro.local)`, etc. Note that recent versions of Unity
     * now include the value of the `UnityEngine.RuntimePlatform` enum in the ID: `OSXPlayer(1,Matts-MBP.local)`.
     *
     * For other players, the value should maintain the same format:
     * * Custom players (user created) - `CustomPlayer({host}:{port})`
     * * iOS USB - TBD
     * * Android ADB - TBD
     */
    var playerId by string("")

    var host by string("localhost")
    var port by property(56000)

    /** The project name, if available.
     *
     * Can be used as part of heuristics to refresh the player's debugger host and port values.
     */
    var projectName by string()
}

// TODO: Implement getIcon to provide a different icon per player type (default is the factory icon)?
class UnityPlayerDebugConfiguration(project: Project, factory: UnityAttachToPlayerFactory) :
    RunConfigurationBase<UnityPlayerDebugConfigurationOptions>(project, factory, null),
    RunConfigurationWithSuppressedDefaultRunAction,
    WithoutOwnBeforeRunSteps,
    IRiderDebuggable {

    // I don't know why RunConfigurationBase has this as nullable
    override fun getState(): UnityPlayerDebugConfigurationOptions = options as UnityPlayerDebugConfigurationOptions

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID)
            return null

        val remoteConfiguration = object: RemoteConfiguration {
            override var address = state.host!!
            override var port = state.port
            override var listenPortForConnections = false
        }
        return UnityAttachProfileState(remoteConfiguration, environment, name, false)
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> = CustomPlayerSettingsEditor()
}

private class CustomPlayerSettingsEditor : SettingsEditor<UnityPlayerDebugConfiguration>() {
    private val form = MonoConnectRemoteForm()

    override fun resetEditorFrom(config: UnityPlayerDebugConfiguration) {
        val state = config.state
        form.hostField.text = state.host
        form.portField.number = state.port
        form.listenForConnectionsBox.isVisible = false
    }

    override fun applyEditorTo(config: UnityPlayerDebugConfiguration) {
        val state = config.state
        state.host = form.hostField.text
        state.port = form.portField.number
    }

    override fun createEditor(): JComponent = form.rootPanel
}