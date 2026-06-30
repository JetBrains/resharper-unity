package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.CantRunException
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunConfigurationBase
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.configurations.WithoutOwnBeforeRunSteps
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.RunConfigurationWithSuppressedDefaultRunAction
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.platform.ide.progress.withBackgroundProgress
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.AndroidDeviceListener
import com.jetbrains.rider.plugins.unity.run.UnityAndroidAdbPlayer
import com.jetbrains.rider.plugins.unity.run.UnityDebugEngine
import com.jetbrains.rider.plugins.unity.run.UnityDebugTarget
import com.jetbrains.rider.plugins.unity.run.UnityDebugTargetKind
import com.jetbrains.rider.plugins.unity.run.UnityEditor
import com.jetbrains.rider.plugins.unity.run.UnityEditorHelper
import com.jetbrains.rider.plugins.unity.run.UnityEditorListener
import com.jetbrains.rider.plugins.unity.run.UnityLocalPlayer
import com.jetbrains.rider.plugins.unity.run.UnityLocalProcess
import com.jetbrains.rider.plugins.unity.run.UnityPlayerListener
import com.jetbrains.rider.plugins.unity.run.UnityVirtualPlayer
import com.jetbrains.rider.run.RiderRunBundle
import com.jetbrains.rider.run.configurations.AsyncRunConfiguration
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.jetbrains.concurrency.Promise

abstract class UnityRunConfigurationBase(project: Project,
                                     factory: UnityConfigurationFactoryBase,
                                     name: String? = null)
    : RunConfigurationBase<UnityPlayerDebugConfigurationOptions>(project, factory, name),
      RunConfigurationWithSuppressedDefaultRunAction,
      AsyncRunConfiguration,
      WithoutOwnBeforeRunSteps,
      IRiderDebuggable
{
    companion object {
        private val logger = Logger.getInstance(UnityRunConfigurationBase::class.java)
    }
    // I don't know why RunConfigurationBase has this as nullable
    override fun getState(): UnityPlayerDebugConfigurationOptions = options as UnityPlayerDebugConfigurationOptions

    protected suspend fun getRunProfileStateAsyncInternal(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        if (executor.id != DefaultDebugExecutor.EXECUTOR_ID) {
            @Suppress("HardCodedStringLiteral")
            throw CantRunException("Unexpected executor ID: ${executor.id}")
        }

        // Get the run profile from the current run configuration state. This will refresh any transient information in the run config such
        // as port numbers or process IDs before creating the appropriate run profile.
        return when (UnityDebugTargetKind.fromPlayerId(state.playerId)) {
            UnityDebugTargetKind.IPhoneUsbPlayer -> getIosUsbState(environment)
            UnityDebugTargetKind.AndroidAdbPlayer -> getAndroidAdbStateAsync(environment)
            UnityDebugTargetKind.UwpPlayer -> getLocalUwpStateAsync(environment)
            UnityDebugTargetKind.Editor -> getEditorStateAsync(environment)
            UnityDebugTargetKind.EditorHelper -> getEditorHelperStateAsync(environment)
            UnityDebugTargetKind.VirtualPlayer -> getVirtualPlayerStateAsync(environment)
            UnityDebugTargetKind.CustomMonoPlayer -> getCustomPlayerState(environment)
            UnityDebugTargetKind.GenericPlayer -> getGenericPlayerStateAsync(environment)
        }
    }

    @Suppress("UsagesOfObsoleteApi")
    @Deprecated("Please, override 'getRunProfileStateAsync' instead")
    override fun getStateAsync(executor: Executor, environment: ExecutionEnvironment): Promise<RunProfileState> {
        @Suppress("DEPRECATION")
        throw UnsupportedOperationException(
            RiderRunBundle.message("obsolete.synchronous.api.is.used.message", UnityPlayerDebugConfiguration::getStateAsync.name))
    }

    override fun getState(executor: Executor, environment: ExecutionEnvironment): Nothing =
        throw UnsupportedOperationException("Synchronous call to getState is not supported")


    override fun hideDisabledExecutorButtons(): Boolean = true

    private fun getIosUsbState(environment: ExecutionEnvironment): RunProfileState {
        // There is nothing to refresh as we use hardcoded forwarded port details. We can't even tell if the player is
        // running, only if the device is connected or not. It's not worth checking this now - we'll just try and attach
        // the debugger to the player and let it fail at that point.
        return UnityAttachIosUsbProfileState(
            project,
            UnityDebugEngine.Mono(state.host!!, state.port),
            environment,
            name,
            state.deviceId!!
        )
    }

    private suspend fun getAndroidAdbStateAsync(environment: ExecutionEnvironment): RunProfileState {
        // Get a list of all running players on the given Android device. If found, connect, else throw an exception
        return withBackgroundProgress(environment.project, UnityBundle.message("debugging.refreshing.player.list"), false) {
            val players = AndroidDeviceListener().getPlayersForDevice(environment.project, state.deviceId!!)

            // Try to find the expected package on the current device. We use the UID which is a unique user ID that is assigned when the
            // package is installed. If the package has been reinstalled (uninstalled + reinstalled, not just redeployed), it will have a
            // new UID. Try again using the package name, if available. If it is not available now, it wouldn't be available when the run
            // config was created, so we'll match on null.
            val player = players.filterIsInstance<UnityAndroidAdbPlayer>().firstOrNull {
                it.deviceId == state.deviceId && it.packageUid == state.androidPackageUid
            } ?: players.filterIsInstance<UnityAndroidAdbPlayer>().firstOrNull {
                it.deviceId == state.deviceId && it.packageName == state.packageName
            }

            if (player != null && player.debugEngine is UnityDebugEngine.Mono) {
                state.host = player.debugEngine.host
                state.port = player.debugEngine.port

                return@withBackgroundProgress UnityAttachAndroidAdbProfileState(
                    project,
                    player.debugEngine,
                    environment,
                    name,
                    state.deviceId!!
                )
            }
            else {
                logger.warn("Cannot find Android player ${state.playerId} in ${players.size} candidates")
                players.forEach { logger.info(it.dump()) }
                throw CantRunException(UnityBundle.message("debugging.cannot.find.android.player"))
            }
        }
    }

    private suspend fun getLocalUwpStateAsync(environment: ExecutionEnvironment): RunProfileState {
        refreshStateFromUdpBroadcast(environment)
        return UnityAttachLocalUwpProfileState(getDebugEngine(), environment, name, state.packageName!!)
    }

    private fun getCustomPlayerState(environment: ExecutionEnvironment): RunProfileState {
        // The user entered these details. There's nothing to refresh, so just try to connect
        return UnityAttachProfileState(UnityDebugEngine.Mono(state.host!!, state.port), environment, name)
    }

    private suspend fun getEditorStateAsync(environment: ExecutionEnvironment): RunProfileState {
        // Refresh the port from the process list
        return getLocalProcessStateAsync(environment, "debugging.cannot.find.editor") { processes ->
            processes.filterIsInstance<UnityEditor>().firstOrNull {
                // Exact match. Previously running instance
                it.processId == state.pid && it.projectName == state.projectName
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

    private suspend fun getEditorHelperStateAsync(environment: ExecutionEnvironment): RunProfileState {
        // Refresh the port from the process list
        return getLocalProcessStateAsync(environment, "debugging.cannot.find.editor.helper") { processes ->
            processes.filterIsInstance<UnityEditorHelper>().firstOrNull {
                // Exact match. Previously running instance
                it.processId == state.pid && it.projectName == state.projectName && it.roleName == state.roleName
            }
            ?: processes.filterIsInstance<UnityEditorHelper>().firstOrNull {
                // A matching helper for the correct project
                // Editor helpers *should* always have a valid project name, but it's not guaranteed, so there's
                // a small but unlikely chance that this will return a helper for the wrong project
                it.roleName == state.roleName && it.projectName == state.projectName
            }
        }
    }

    private suspend fun getVirtualPlayerStateAsync(environment: ExecutionEnvironment): RunProfileState {
        // Refresh the port from the process list
        return getLocalProcessStateAsync(environment, "debugging.cannot.find.virtual.player") { processes ->
            processes.filterIsInstance<UnityVirtualPlayer>().firstOrNull {
                // Exact match. Previously running instance
                it.processId == state.pid && it.projectName == state.projectName && it.playerName == state.virtualPlayerName
            }
            ?: processes.filterIsInstance<UnityVirtualPlayer>().firstOrNull {
                // Try to find an editor instance that is hosting the target virtual player
                it.virtualPlayerId == state.virtualPlayerId && it.projectName == state.projectName
            }
        }
    }

    private suspend fun getLocalProcessStateAsync(environment: ExecutionEnvironment,
                                                  errorMessage: String,
                                                  processFinder: (List<UnityDebugTarget>) -> UnityDebugTarget?): RunProfileState {
        // Refresh the port from the process list
        return withBackgroundProgress(environment.project, UnityBundle.message("debugging.refreshing.player.list"), false) {
            val processes = withContext(Dispatchers.Default) { UnityEditorListener().getEditorProcesses(environment.project) }
            val process = processFinder(processes)
            if (process != null && process.debugEngine is UnityDebugEngine.Mono && process is UnityLocalProcess) {
                // Process ID isn't actually used while debugging, but it can help to locate the process when reattaching
                state.pid = process.processId
                state.host = process.debugEngine.host
                state.port = process.debugEngine.port

                return@withBackgroundProgress UnityAttachProfileState(
                    process.debugEngine,
                    environment,
                    name,
                    isEditor = true
                )
            }
            else {
                logger.warn("Cannot find local process player ${state.playerId} in ${processes.size} candidates")
                processes.forEach { logger.info(it.dump()) }
                throw CantRunException(UnityBundle.message(errorMessage))
            }
        }
    }

    private suspend fun getGenericPlayerStateAsync(environment: ExecutionEnvironment): RunProfileState {
        refreshStateFromUdpBroadcast(environment)
        return UnityAttachProfileState(getDebugEngine(), environment, name)
    }

    private suspend fun refreshStateFromUdpBroadcast(environment: ExecutionEnvironment) {
        withBackgroundProgress(environment.project, UnityBundle.message("debugging.refreshing.player.list"), false) {
            val candidates = mutableSetOf<String>()

            // Get the player. We're only interested in a player that matches
            // the ID (e.g. 'OSXPlayer(1,Matts-macbook)'), projectName, and playerInstanceId
            val player = UnityPlayerListener().getPlayer(environment.project) {
                candidates.add(it.dump())
                it.id == state.playerId &&
                it.projectName == state.projectName &&
                it.playerInstanceId == state.playerInstanceId
            }

            if (player == null) {
                logger.warn("Cannot find UDP player ${state.playerId} in ${candidates.size} candidates")
                candidates.forEach { logger.info(it) }
                throw CantRunException(UnityBundle.message("debugging.cannot.find.udp.player"))
            }
            if (player !is UnityLocalPlayer && player.debugEngine is UnityDebugEngine.CoreClr) {
                throw CantRunException(UnityBundle.message("debugging.cannot.debug.coreclr.player"))
            }

            when (player.debugEngine) {
                is UnityDebugEngine.CoreClr -> {
                    state.isCoreClr = true
                    state.pid = player.debugEngine.processId
                }

                is UnityDebugEngine.Mono -> {
                    state.isCoreClr = false
                    state.host = player.debugEngine.host
                    state.port = player.debugEngine.port
                }
            }
        }
    }

    internal fun getDebugEngine(): UnityDebugEngine {
        return if (state.isCoreClr) UnityDebugEngine.CoreClr(state.pid) else UnityDebugEngine.Mono(state.host!!, state.port)
    }
}
