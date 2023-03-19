package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.CantRunException
import com.intellij.execution.Executor
import com.intellij.execution.configurations.*
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.startUnderBackgroundProgressAsync
import com.intellij.openapi.rd.util.toPromise
import com.intellij.ui.dsl.builder.panel
import com.intellij.ui.layout.ComponentPredicate
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.run.configurations.AsyncRunConfiguration
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteForm
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import icons.UnityIcons
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.jetbrains.concurrency.Promise
import org.jetbrains.concurrency.resolvedPromise
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
    AsyncRunConfiguration,
    WithoutOwnBeforeRunSteps,
    IRiderDebuggable {

    // I don't know why RunConfigurationBase has this as nullable
    override fun getState(): UnityPlayerDebugConfigurationOptions = options as UnityPlayerDebugConfigurationOptions

    override fun getStateAsync(executor: Executor, environment: ExecutionEnvironment): Promise<RunProfileState> {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID) {
            @Suppress("HardCodedStringLiteral")
            throw CantRunException("Unexpected executor ID: ${executor.id}")
            // TODO: We should be able to return resolvedPromise(null), but the function's type doesn't allow this
        }

        return when (UnityProcess.typeFromId(state.playerId!!)) {
            UnityIosUsbProcess.TYPE -> getIosUsbStateAsync(environment)
            UnityAndroidAdbProcess.TYPE -> getAndroidAdbStateAsync(environment)
            UnityLocalUwpPlayer.TYPE -> getLocalUwpStateAsync(environment)
            UnityCustomPlayer.TYPE -> getCustomPlayerStateAsync(environment)
            UnityEditor.TYPE -> getEditorStateAsync(environment)
            UnityEditorHelper.TYPE -> getEditorHelperStateAsync(environment)
            else -> getRemotePlayerStateAsync(environment)
        }
    }

    override fun getState(executor: Executor, environment: ExecutionEnvironment) =
      throw UnsupportedOperationException("Synchronous call to getState is not supported")

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> {
        val type = UnityProcess.typeFromId(state.playerId!!)
        if (type == UnityCustomPlayer.TYPE) {
            return CustomPlayerSettingsEditor()
        }

        return UnityPlayerSettingsEditor()
    }

    override fun hideDisabledExecutorButtons() = true

    private fun getIosUsbStateAsync(environment: ExecutionEnvironment): Promise<RunProfileState> {
        // There is nothing to refresh as we use hardcoded forwarded port details. We can't even tell if the player is
        // running, only if the device is connected or not. It's not worth checking this now - we'll just try and attach
        // the debugger to the player and let it fail at that point.
        return resolvedPromise(
            UnityAttachIosUsbProfileState(
                project,
                getRemoteConfiguration(),
                environment,
                name,
                state.deviceId!!
            )
        )
    }

    @OptIn(ExperimentalCoroutinesApi::class)
    private fun getAndroidAdbStateAsync(environment: ExecutionEnvironment): Promise<RunProfileState> {
        // Get a list of all running players on the given Android device. If found, connect, else throw an exception
        return environment.project.lifetime.startUnderBackgroundProgressAsync(
            UnityBundle.message("debugging.refreshing.player.list"),
            canBeCancelled = false,
            isIndeterminate = true,
            project = environment.project
        ) {
            val players = AndroidDeviceListener().getPlayersForDevice(environment.project, state.deviceId!!)
            val player = players.filterIsInstance<UnityAndroidAdbProcess>().firstOrNull {
                it.deviceId == state.deviceId && it.packageUid == state.androidPackageUid
            }
            if (player != null) {
                state.host = player.host
                state.port = player.port

                return@startUnderBackgroundProgressAsync UnityAttachAndroidAdbProfileState(
                    project,
                    getRemoteConfiguration(),
                    environment,
                    name,
                    state.deviceId!!
                )
            }
            else {
                throw CantRunException(UnityBundle.message("debugging.cannot.find.android.player"))
            }
        }.toPromise()
    }

    private fun getLocalUwpStateAsync(environment: ExecutionEnvironment): Promise<RunProfileState> {
        // Refresh the details from the UDP broadcast, and use the UWP specific RunProfileState
        return refreshFromUdpPlayer(environment) {
            UnityAttachLocalUwpProfileState(
                getRemoteConfiguration(),
                environment,
                name,
                state.packageName!!
            )
        }
    }

    private fun getCustomPlayerStateAsync(environment: ExecutionEnvironment): Promise<RunProfileState> {
        // The user entered these details. There's nothing to refresh, so just try to connect
        return resolvedPromise(
            UnityAttachProfileState(
                getRemoteConfiguration(),
                environment,
                name
            )
        )
    }

    private fun getEditorStateAsync(environment: ExecutionEnvironment): Promise<RunProfileState> {
        // Refresh the port from the process list
        return getLocalProcessStateAsync(environment, "debugging.cannot.find.editor") { processes ->
            processes.filterIsInstance<UnityEditor>().firstOrNull {
                // Exact match. Previously running instance
                it.pid == state.pid && it.projectName == state.projectName
            }
            ?: processes.filterIsInstance<UnityEditor>().firstOrNull {
                // A matching editor for the correct project
                // We are highly likely to know the project name of the editor running for the current project, but that
                // will use the "Attach to Unity Editor" run config, not the player debug. Which means we're (probably)
                // an editor for another project, which is unexpected (developing a package in one project and debugging
                // in a game?). We can get the project name in many circumstances, but not all, so there's a chance
                // we're comparing with null and will return an arbitrary editor
                it.projectName == state.projectName
            }
        }
    }

    private fun getEditorHelperStateAsync(environment: ExecutionEnvironment): Promise<RunProfileState> {
        // Refresh the port from the process list
        return getLocalProcessStateAsync(environment, "debugging.cannot.find.editor.helper") { processes ->
            processes.filterIsInstance<UnityEditorHelper>().firstOrNull {
                // Exact match. Previously running instance
                it.pid == state.pid && it.projectName == state.projectName && it.roleName == state.roleName
            }
            ?: processes.filterIsInstance<UnityEditorHelper>().firstOrNull {
                // A matching helper for the correct project
                // Editor helpers *should* always have a valid project name, but it's not guaranteed, so there's
                // a small but unlikely chance that this will return a helper for the wrong project
                it.roleName == state.roleName && it.projectName == state.projectName
            }
        }
    }

    @OptIn(ExperimentalCoroutinesApi::class)
    private fun getLocalProcessStateAsync(environment: ExecutionEnvironment,
                                          errorMessage: String,
                                          processFinder: (List<UnityProcess>) -> UnityLocalProcess?): Promise<RunProfileState> {
        // Refresh the port from the process list
        return environment.project.lifetime.startUnderBackgroundProgressAsync(
            UnityBundle.message("debugging.refreshing.player.list"),
            canBeCancelled = false,
            isIndeterminate = true,
            project = environment.project
        ) {
            val processes = UnityEditorListener().getEditorProcesses(environment.project)
            val process = processFinder(processes)
            if (process != null) {
                state.host = process.host
                state.port = process.port
                state.pid = process.pid

                return@startUnderBackgroundProgressAsync UnityAttachProfileState(
                    getRemoteConfiguration(),
                    environment,
                    name,
                    isEditor = true
                )
            }
            else {
                throw CantRunException(UnityBundle.message(errorMessage))
            }
        }.toPromise()
    }

    private fun getRemotePlayerStateAsync(environment: ExecutionEnvironment): Promise<RunProfileState> {
        return refreshFromUdpPlayer(environment) {
            UnityAttachProfileState(
                getRemoteConfiguration(),
                environment,
                name
            )
        }
    }

    @OptIn(ExperimentalCoroutinesApi::class)
    private fun refreshFromUdpPlayer(environment: ExecutionEnvironment, factory: () -> UnityAttachProfileState): Promise<RunProfileState> {
        // Refresh state from the UDP broadcast
        return environment.project.lifetime.startUnderBackgroundProgressAsync(
            UnityBundle.message("debugging.refreshing.player.list"),
            canBeCancelled = false,
            isIndeterminate = true,
            project = environment.project
        ) {

            // Get the player. We're only interested in a player that matches the ID (e.g. 'OSXPlayer(1,Matts-macbook)')
            // and that matches the project name
            val player = UnityPlayerListener().getPlayer(environment.project) {
                it.id == state.playerId && it.projectName == state.projectName
            }

            if (player != null) {
                state.host = player.host
                state.port = player.port

                return@startUnderBackgroundProgressAsync factory()
            }
            else {
                throw CantRunException(UnityBundle.message("debugging.cannot.find.udp.player"))
            }
        }.toPromise()
    }

    private fun getRemoteConfiguration() = object : RemoteConfiguration {
        override var address = state.host!!
        override var port = state.port
        override var listenPortForConnections = false
    }
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