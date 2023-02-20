package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.ui.dsl.builder.panel
import com.intellij.ui.layout.ComponentPredicate
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteForm
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import icons.UnityIcons
import javax.swing.JComponent
import javax.swing.JLabel

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
     * * iOS USB - `iPhoneUSBPlayer({deviceId})`
     * * Android ADB - `AndroidADBPlayer({deviceId})`
     *
     * See also [UnityProcess.type].
     */
    var playerId by string("")

    var host by string("localhost")
    var port by property(56000)

    /** The iOS or Android ADB device ID */
    var deviceId by string("")

    /* Friendly device name for Android and iOS */
    var deviceName by string("")

    /**
     * The name of the Android or UWP package
     *
     * Can be null for Android package.
     */
    var packageName by string("")

    /** The UID of the Android package */
    var androidPackageUid: String? by string("")

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

    // TODO: Implement AsyncRunConfiguration.getStateAsync to allow updating remote configuration settings
    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState? {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID)
            return null

        val remoteConfiguration = object: RemoteConfiguration {
            override var address = state.host!!
            override var port = state.port
            override var listenPortForConnections = false
        }

        return when (val playerType = UnityProcess.typeFromId(state.playerId!!)) {
            UnityIosUsbProcess.TYPE -> UnityAttachIosUsbProfileState(
                project,
                remoteConfiguration,
                environment,
                name,
                state.deviceId!!
            )
            UnityAndroidAdbProcess.TYPE -> UnityAttachAndroidAdbProfileState(
                project,
                remoteConfiguration,
                environment,
                name,
                state.deviceId!!
            )
            UnityLocalUwpPlayer.TYPE -> UnityAttachLocalUwpProfileState(
                remoteConfiguration,
                environment,
                name,
                state.packageName!!
            )
            else -> UnityAttachProfileState(
                remoteConfiguration,
                environment,
                name,
                playerType == UnityEditor.TYPE || playerType == UnityEditorHelper.TYPE
            )
        }
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> {
        val type = UnityProcess.typeFromId(state.playerId!!)
        if (type == UnityCustomPlayer.TYPE) {
            return CustomPlayerSettingsEditor()
        }

        return UnityPlayerSettingsEditor()
    }

    override fun hideDisabledExecutorButtons() = true
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

private class UnityPlayerSettingsEditor : SettingsEditor<UnityPlayerDebugConfiguration>() {

    private val panel = panel {
        // There is nothing user editable, but show the details of the player that we'll use to find the host/port
        row(UnityBundle.message("run.configuration.player.label.id")) { id = this.label("").component }
        row(UnityBundle.message("run.configuration.player.label.address")) { address = label("").component }
        // Only available in 2019.3 and above. Not applicable to iOS or Android ADB
        row(UnityBundle.message("run.configuration.player.label.project")) { projectName = label("").component }.visibleIf(HasNonEmptyText(projectName))
        // if Android/iOS
        row(UnityBundle.message("run.configuration.player.label.deviceId")) { deviceId = label("").component }.visibleIf(HasNonEmptyText(deviceId))
        // Device name?
        row(UnityBundle.message("run.configuration.player.label.deviceName")) { deviceName = label("").component }.visibleIf(HasNonEmptyText(deviceName))
        // if Android/UWP
        row(UnityBundle.message("run.configuration.player.label.packageName")) { packageName = label("").component }.visibleIf(HasNonEmptyText(packageName))

        // TODO: Show all players, like in the picker dialog, but readonly. Select the current player, if available
    }

    private lateinit var id: JLabel
    private lateinit var address: JLabel
    private lateinit var projectName: JLabel
    private lateinit var deviceId: JLabel
    private lateinit var deviceName: JLabel
    private lateinit var packageName: JLabel

    override fun resetEditorFrom(config: UnityPlayerDebugConfiguration) {
        val state = config.state
        id.text = state.playerId
        address.text = "${state.host}:${state.port}"
        projectName.text = state.projectName
        deviceId.text = state.deviceId
        deviceName.text = state.deviceName
        packageName.text = state.packageName
    }

    override fun applyEditorTo(config: UnityPlayerDebugConfiguration) {
        // Nothing editable
    }

    override fun createEditor(): JComponent = panel

    private class HasNonEmptyText(private val label: JLabel) : ComponentPredicate() {
        override fun invoke() = !label.text.isNullOrEmpty()
        override fun addListener(listener: (Boolean) -> Unit) {
            label.addPropertyChangeListener("text") { listener(invoke()) }
        }
    }
}