package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.Executor
import com.intellij.execution.configurations.ConfigurationType
import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.configurations.RunConfigurationOptions
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.configurations.VirtualConfigurationType
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.ui.dsl.builder.panel
import com.intellij.ui.layout.ComponentPredicate
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.UnityCustomPlayer
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteForm
import icons.UnityIcons
import javax.swing.JComponent
import javax.swing.JLabel

internal class UnityPlayerDebugConfigurationType : ConfigurationTypeBase(
    ID,
    UnityBundle.message("configuration.type.name.attach.to.unity.player"),
    UnityBundle.message("configuration.type.description.attach.to.unity.player.and.debug"),
    UnityIcons.RunConfigurations.AttachToPlayer
), VirtualConfigurationType {

    val attachToPlayerFactory = UnityAttachToPlayerFactory(this)

    init {
        addFactory(attachToPlayerFactory)
    }

    companion object {
        const val ID = "UnityPlayer"
    }
}

class UnityAttachToPlayerFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {
    override fun createTemplateConfiguration(project: Project) =
        UnityPlayerDebugConfiguration(project, this)

    override fun getId() = "UnityAttachToPlayer"
    override fun getOptionsClass() = UnityPlayerDebugConfigurationOptions::class.java
}

class UnityPlayerDebugConfigurationOptions : RunConfigurationOptions() {

    /**
     * A string to identify a player type on a particular host
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
     * Note that the player ID is not a unique value. It mainly identifies a player type running on a specific host, but does not identify
     * which project the player type is running. It is possible to have multiple players of the same type, on the same host running
     * different projects. Use [playerInstanceId] to distinguish between multiple instances/projects on the same host/player.
     *
     * See also [UnityProcess.type].
     */
    var playerId by string("")

    /**
     * A string used to disambiguate player projects running on the same project
     *
     * By default, this is the same as [projectName], but that isn't available on all platforms, such as Android. In which case, the player
     * can provide a different value. For Android, this is [packageName], if available.
     *
     * Note that this value was only introduced in Rider 2023.2.2, so will not be serialised with run configurations created before then.
     */
    var playerInstanceId by string()

    var host by string("localhost")
    var port by property(56000)

    /** The local process (Editor/editor helper) process ID */
    var pid by property(0)

    var roleName by string("")

    /** The iOS or Android ADB device ID */
    var deviceId by string("")

    /* Friendly device name for Android and iOS */
    var deviceName by string("")

    /**
     * The name of the Android or UWP package
     *
     * Can be null for an Android package.
     */
    var packageName by string("")

    /** The UID of the Android package */
    var androidPackageUid by string("")

    /** The project name, if available */
    var projectName by string()

    /**
     * The virtual player ID, e.g. `mppmca3577a6`
     *
     * This allows matching a virtual player even if the descriptive player name has changed
     */
    var virtualPlayerId by string()

    var virtualPlayerName by string()
}

// TODO: Implement getIcon to provide a different icon per player type (default is the factory icon)?
open class UnityPlayerDebugConfiguration(project: Project, factory: UnityAttachToPlayerFactory) :
    UnityRunConfigurationBase(project, factory) {
    override suspend fun getRunProfileStateAsync(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        return getRunProfileStateAsyncInternal(executor, environment)
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> {
        val type = UnityProcess.typeFromId(state.playerId!!)
        if (type == UnityCustomPlayer.TYPE) {
            return CustomPlayerSettingsEditor()
        }

        return UnityPlayerSettingsEditor()
    }
}

private class CustomPlayerSettingsEditor : SettingsEditor<UnityPlayerDebugConfiguration>() {
    private val form = MonoConnectRemoteForm()
        .also { /*we can implement mixed mode for this editor, if we need to*/it.useMixedModeCheckbox.isVisible = false }

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
        row(UnityBundle.message("run.configuration.player.label.project")) { projectName = label("").component }.visibleIf(
            HasNonEmptyText(projectName))
        row(UnityBundle.message("run.configuration.player.label.role")) { roleName = label("").component }.visibleIf(
            HasNonEmptyText(roleName))
        row(UnityBundle.message("run.configuration.player.label.virtualPlayerName")) { virtualPlayerName = label("").component }.visibleIf(
            HasNonEmptyText(virtualPlayerName))
        row(UnityBundle.message("run.configuration.player.label.virtualPlayerId")) { virtualPlayerId = label("").component }.visibleIf(
            HasNonEmptyText(virtualPlayerId))
        // if Android/iOS
        row(UnityBundle.message("run.configuration.player.label.deviceId")) { deviceId = label("").component }.visibleIf(
            HasNonEmptyText(deviceId))
        // Device name?
        row(UnityBundle.message("run.configuration.player.label.deviceName")) { deviceName = label("").component }.visibleIf(
            HasNonEmptyText(deviceName))
        // if Android/UWP. Might be null on Android if we've not been able to collect it
        row(UnityBundle.message("run.configuration.player.label.packageName")) { packageName = label("").component }.visibleIf(
            HasNonEmptyText(packageName))

        // It might be nice to show a list of current players, like in the "Attach to Unity Process" dialog, highlighting this player
    }

    private lateinit var id: JLabel
    private lateinit var address: JLabel
    private lateinit var projectName: JLabel
    private lateinit var roleName: JLabel
    private lateinit var virtualPlayerId: JLabel
    private lateinit var virtualPlayerName: JLabel
    private lateinit var deviceId: JLabel
    private lateinit var deviceName: JLabel
    private lateinit var packageName: JLabel

    override fun resetEditorFrom(config: UnityPlayerDebugConfiguration) {
        val state = config.state
        id.text = state.playerId
        address.text = "${state.host}:${state.port}"
        projectName.text = state.projectName
        roleName.text = state.roleName
        virtualPlayerId.text = state.virtualPlayerId
        virtualPlayerName.text = state.virtualPlayerName
        deviceId.text = state.deviceId
        deviceName.text = state.deviceName
        packageName.text = state.packageName + if (!state.androidPackageUid.isNullOrEmpty()) " (${state.androidPackageUid})" else ""
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