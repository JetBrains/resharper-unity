@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort
import icons.UnityIcons
import javax.swing.Icon

/**
 * Identifies the debug target kind based on a given player ID prefix
 *
 * Note that the player ID prefix does NOT match the player ID prefix given in the UDP broadcast, unless otherwise specified. Many of the
 * given debug targets are not included in the UDP broadcast, e.g., editor processes, iOS and Android via USB, etc.
 */
enum class UnityDebugTargetKind(val kind: String) {
    /**
     * Identifies a player that requires no specific handling and is identified by the UDP broadcast
     */
    GenericPlayer("GenericPlayer"),

    IPhoneUsbPlayer("iPhoneUSBPlayer"),
    AndroidAdbPlayer("AndroidAdbPlayer"),

    /**
     * Identifies a player that is build using Windows's UWP platform
     *
     * The text is the name of the player that Unity uses, e.g. `UWPPlayer(...)`
     */
    UwpPlayer("UWPPlayer"),

    Editor("Editor"),
    EditorHelper("EditorHelper"),
    VirtualPlayer("VirtualPlayer"),

    /**
     * Identifies a player configuration configured manually to connect via the Mono debug protocol
     */
    CustomMonoPlayer("CustomPlayer");

    companion object {
        fun fromPlayerId(playerId: String?): UnityDebugTargetKind {
            if (playerId == null) return GenericPlayer
            return entries.firstOrNull { playerId.startsWith(it.kind) } ?: GenericPlayer
        }
    }
}

/**
 * Describes the debug engine and parameters for this debug target
 *
 * Unity has traditionally been built on Mono and uses the Mono debug protocol for debugging players, editors, and editor helpers, even when
 * the player has been built with IL2CPP. Unity is slowly transitioning to CoreCLR, both for players and editors. Rider will have to support
 * both debug engines for some time to come.
 */
sealed class UnityDebugEngine {
    abstract fun toPresentableString(): String
    val kind: String = this::class.simpleName!!

    data class Mono(val host: String, val port: Int) : UnityDebugEngine() {
        /**
         * Create new Mono debug parameters based on the process ID of a local process
         */
        internal constructor(pid: Int) : this("127.0.0.1", convertPidToDebuggerPort(pid))

        override fun toPresentableString(): String = "$host:$port"
    }

    data class CoreClr(val processId: Int) : UnityDebugEngine() {
        override fun toPresentableString(): String = "pid: $processId"
    }
}

/**
 * The base class that represents a Unity process that can potentially be debugged
 *
 * All Unity processes can be debugged via the Mono protocol, so all require a host and port, even if some players
 * (such as editors and iOS over USB) hard code one or both of the values.
 * Where possible, the player should provide a project name to help disambiguate players.
 *
 * @param id  A string uniquely representing the Unity process or player. Will be in the format
 *                  `platformPlayer(identifier)`. See different player types for more details.
 */
sealed class UnityDebugTarget(
    val id: String,
    @NlsSafe val name: String,
    val debuggingEnabled: Boolean,
    val debugEngine: UnityDebugEngine,
    @NlsSafe open val projectName: String? = null,
    val icon: Icon = UnityIcons.RunConfigurations.AttachToPlayer
) {
    /**
     * A non-user-facing string used to distinguish different projects running on the same player/host
     *
     * We need to be able to identify a player so that we can attach, save details in a run configuration and easily reattach, even if the
     * player has been restarted. If the player has not been restarted, we maintain the debug information (host/port or process ID) and can
     * use that to find the player. If the player has been restarted, we need heuristics.
     *
     * The simplest method is to check the project name, if available. However, Android doesn't have a project name, so we use the package
     * name instead. This property allows us to provide a value without needing to know where it comes from.
     *
     * Note that we don't have enough stable information to distinguish between two players running the same project on the same host. Also
     * note that if there's no project name, this value will be null (and we can't guarantee the Android package name either).
     *
     * debugEngine was added to distinguish Player with different debug engines.
     */
    open val playerInstanceId: String? = projectName?.let { "$it#${debugEngine.kind}" }

    open fun dump(): String =
        "$id ($name, ${debugEngine.toPresentableString()}, debugging ${if (debuggingEnabled) "enabled" else "disabled"}, ${projectName ?: "no project name"})"

    companion object {
        @JvmStatic
        protected fun getSafeProjectName(projectName: String?): String = projectName ?: "UnknownProject"
    }
}

/**
 * Identifies a debug target that is running locally on the current machine
 */
interface UnityLocalProcess {
    val processId: Int
}

val UnityDebugTarget.processIdOrZero: Int
    get() = (this as? UnityLocalProcess)?.processId ?: 0

// CoreCLR is currently only supported in local players
val UnityDebugTarget.isDebuggerSupported: Boolean
    get() = debugEngine is UnityDebugEngine.Mono || this is UnityLocalPlayer

/**
 * A Unity editor instance
 *
 * The user might have multiple of these.
 * The project name can help disambiguate, and the current project's editor will save the process ID into the project's
 * `Library/EditorInstance.json` file.
 */
class UnityEditor(executableName: String, override val processId: Int, projectName: String?) :
    UnityDebugTarget(
        "${UnityDebugTargetKind.Editor.kind}($executableName-${getSafeProjectName(projectName)})",
        executableName,
        debuggingEnabled = true,
        UnityDebugEngine.Mono(processId),
        projectName
    ), UnityLocalProcess

/**
 * A helper process for an editor, such as an asset importer
 *
 * The display name is the helper's role, e.g. "AssetImportWorker0", and the project name will be used to group with its
 * parent editor.
 * The project name should always be available, but this is not guaranteed.
 */
class UnityEditorHelper(executableName: String, @NlsSafe val roleName: String, override val processId: Int, projectName: String?) :
    UnityDebugTarget(
        "${UnityDebugTargetKind.EditorHelper.kind}($executableName-$roleName-${getSafeProjectName(projectName)}",
        executableName,
        debuggingEnabled = true,
        UnityDebugEngine.Mono(processId),
        projectName
    ), UnityLocalProcess

/**
 * A virtual player, when the editor is in multiplayer play mode
 *
 * Each virtual player is a new instance of the editor, with a new sparse copy of the project living inside `Library/VP/{id}`. The `Assets`
 * and `Packages` folders are symlinked to the actual directories, and each player has its own instance of `Library` (complete with files
 * such as `Library/EditorInstance.json`). Each player has an ID such as `mppmca3577a6`, but a display name can be set in the main editor.
 * The details about the players are stored in the main project's `Library/VP/PlayerData.json`, including display names, identifiers, and
 * tags. We don't read this file as we can get all the information we need from the command line of the virtual player's editor.
 */
class UnityVirtualPlayer(
    executableName: String,
    @NlsSafe val playerName: String,
    val virtualPlayerId: String,
    override val processId: Int,
    projectName: String?
) : UnityDebugTarget(
    "${UnityDebugTargetKind.VirtualPlayer.kind}($virtualPlayerId)",
    executableName,
    debuggingEnabled = true,
    UnityDebugEngine.Mono(processId),
    projectName
), UnityLocalProcess

/**
 * Represents a player that is local to the current desktop, such as OSX or Windows player
 *
 * Players are discovered via UDP multicast messages, which are used to derive the debugger host and port.
 * Local players are running on the current desktop, while all other players are by definition remote, and can be
 * running on another desktop, a mobile device, a console, etc.
 * Local players are given more priority than remote players in the UI because you are more likely to be interested in
 * a player on your desktop.
 * Note that we don't hardcode the host, because it can still be a local address even if it's not "127.0.0.1", and using
 * the value that we're given has the best chance of working with the debugger.
 */
open class UnityLocalPlayer(playerId: String, debuggingEnabled: Boolean, debugEngine: UnityDebugEngine, projectName: String?) :
    UnityDebugTarget(playerId, playerId, debuggingEnabled, debugEngine, projectName)

/**
 * A local Windows UWP player
 *
 * UWP processes require special handling for debugging, so need to be recognised separately.
 * Note that we don't hardcode the host, because it can still be a local address even if it's not "127.0.0.1", and using
 * the value that we're given has the best chance of working with the debugger.
 */
class UnityLocalUwpPlayer(
    playerId: String,
    debuggingEnabled: Boolean,
    debugEngine: UnityDebugEngine,
    projectName: String?,
    val packageName: String
) : UnityLocalPlayer(playerId, debuggingEnabled, debugEngine, projectName)

/**
 * Players that are not local to the current desktop
 *
 * This can include standalone players on other machines, mobile devices, consoles, etc.
 */
class UnityRemotePlayer(playerId: String, debuggingEnabled: Boolean, debugEngine: UnityDebugEngine, projectName: String?) :
    UnityDebugTarget(playerId, playerId, debuggingEnabled, debugEngine, projectName) {
}

/**
 * User-created player process with hardcoded host and port
 *
 * If the normal player discovery processes fail, the user can enter a custom host and port.
 * We can save the player and reuse the connection details to prevent the need to continually re-enter the details.
 * We can also assume that the custom player is for the current project, or why else would you debug it?
 */
class UnityCustomPlayer(displayName: String, host: String, port: Int, override val projectName: String) :
    UnityDebugTarget(
        "${UnityDebugTargetKind.CustomMonoPlayer.kind}($host:$port})",
        displayName,
        debuggingEnabled = true,
        UnityDebugEngine.Mono(host, port),
        projectName
    ) {
    override fun dump(): String = "$id ($name, ${debugEngine.toPresentableString()}, $projectName)"

    companion object {
        fun isCustomPlayer(id: String?): Boolean = UnityDebugTargetKind.fromPlayerId(id) == UnityDebugTargetKind.CustomMonoPlayer
    }
}

/**
 * Represents a connection to iOS over USB. Does not necessarily mean that a game is running and ready to be debugged.
 *
 * The host and port are hardcoded because we use a proxy (usbmuxd) to forward a local port to the game's remote port
 * (always 56000) over USB.
 * The local port is hardcoded at 12000 based on Unity's own open source debugger plugins.
 */
class UnityIosUsbPlayer(displayName: String, val deviceId: String, val deviceDisplayName: String) :
    UnityDebugTarget(
        "${UnityDebugTargetKind.IPhoneUsbPlayer.kind}($deviceId)",
        displayName,
        debuggingEnabled = true,
        UnityDebugEngine.Mono("127.0.0.1", 12000),
        projectName = null
    ) {
    override fun dump(): String = "$id ($name, $deviceId, $deviceDisplayName, ${debugEngine.toPresentableString()})"
}

/**
 * Represents a Unity game running on Android discovered via ADB, potentially over USB
 *
 * The game is listening on a port on the remote device that is somewhere between 56000 and 57000, inclusive.
 * To connect the debugger, we tell adb to forward the same local port to the remote port over the ADB connection (USB),
 * and connect the debugger to the local port.
 */
class UnityAndroidAdbPlayer(
    displayName: String,
    val deviceId: String,
    val deviceDisplayName: String?,
    port: Int,
    val packageUid: String,
    val packageName: String?,
    icon: Icon = UnityIcons.RunConfigurations.AttachToPlayer
) : UnityDebugTarget(
    "${UnityDebugTargetKind.AndroidAdbPlayer.kind}($deviceId)",
    displayName,
    debuggingEnabled = true,
    UnityDebugEngine.Mono("127.0.0.1", port),
    projectName = null,
    icon = icon
) {
    override val playerInstanceId: String? = packageName?.let { "$it#${debugEngine.kind}" }

    override fun dump(): String =
        "$id ($name, $deviceId, $deviceDisplayName, ${debugEngine.toPresentableString()}, UID: $packageUid, ${packageName ?: "no package name"}"
}
