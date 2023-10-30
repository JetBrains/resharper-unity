@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort

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
sealed class UnityProcess(
    val id: String,
    @NlsSafe val displayName: String,
    @NlsSafe val host: String,
    val port: Int,
    val debuggingEnabled: Boolean,
    @NlsSafe open val projectName: String? = null
) {
    val type = typeFromId(id)

    /**
     * A name used to distinguish different projects running on the same player/host
     *
     * The player ID is a combination of player type and host details or device ID. It is possible to have multiple instances of a player on
     * a single host, such as multiple players running on a desktop or an Android device. Normally, we can distinguish between instances by
     * looking at the project name, if available, but Android doesn't have the project name, only a package name. This string allows us to
     * provide a different ID based on the current player type.
     *
     * Note that if there is no project name, this value can still be `null`.
     */
    open val playerInstanceId
        get() = projectName

    abstract fun dump(): String

    companion object {
        fun typeFromId(id: String): String = id.substringBefore('(')
    }
}

/** Base class of Unity players which are also local processes, such as the Editor */
sealed class UnityLocalProcess(id: String, name: String, val pid: Int, projectName: String?) :
    UnityProcess(id, name, "127.0.0.1", convertPidToDebuggerPort(pid), true, projectName) {

    override fun dump() =
        "$id ($displayName, $host:$port, debugging ${if (debuggingEnabled) "enabled" else "disabled"}, ${projectName ?: "no project name"})"
}

/**
 * A Unity editor instance
 *
 * The user might have multiple of these.
 * The project name can help disambiguate, and the current project's editor will save the process ID into the project's
 * `Library/EditorInstance.json` file.
 */
class UnityEditor(executableName: String, pid: Int, projectName: String?) :
    UnityLocalProcess("$TYPE($executableName-${projectName ?: "UnknownProject"})", executableName, pid, projectName) {
    companion object {
        const val TYPE = "Editor"
    }
}

/**
 * A helper process for an editor, such as an asset importer
 *
 * The display name is the helper's role, e.g. "AssetImportWorker0", and the project name will be used to group with its
 * parent editor.
 * The project name should always be available, but this is not guaranteed.
 */
class UnityEditorHelper(executableName: String, @NlsSafe val roleName: String, pid: Int, projectName: String?) :
    UnityLocalProcess("$TYPE($executableName-$roleName-${projectName ?: "UnknownProject"})", executableName, pid, projectName) {
    companion object {
        const val TYPE = "EditorHelper"
    }
}

/**
 * A virtual player, when the editor is in multiplayer play mode
 *
 * Each virtual player is a new instance of the editor, with a new sparse copy of the project living inside `Library/VP/{id}`. The `Assets`
 * and `Packages` folders are symlinked to the actual directories, and each player has its own instance of `Library` (complete with files
 * such as `Library/EditorInstance.json`). Each player has an ID such as `mppmca3577a6`, but a display name can be set in the main editor.
 * The details about the players are stored in the main project's `Library/VP/PlayerData.json`, including display names, identifiers, and
 * tags. We don't read this file as we can get all the information we need from the command line of the virtual player's editor.
 */
class UnityVirtualPlayer(executableName: String, @NlsSafe val playerName: String, val virtualPlayerId: String, pid: Int, projectName: String?) :
    UnityLocalProcess("$TYPE($virtualPlayerId)", executableName, pid, projectName) {
    companion object {
        const val TYPE = "VirtualPlayer"
    }
}

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
open class UnityLocalPlayer(
    playerId: String,
    host: String,
    port: Int,
    debuggingEnabled: Boolean,
    projectName: String?
) : UnityProcess(playerId, playerId, host, port, debuggingEnabled, projectName) {

    override fun dump() =
        "$id ($displayName, $host:$port, debugging ${if (debuggingEnabled) "enabled" else "disabled"}, ${projectName ?: "no project name"})"
}

/**
 * A local Windows UWP player
 *
 * UWP processes require special handling for debugging, so need to be recognised separately.
 * Note that we don't hardcode the host, because it can still be a local address even if it's not "127.0.0.1", and using
 * the value that we're given has the best chance of working with the debugger.
 */
class UnityLocalUwpPlayer(
    playerId: String,
    host: String,
    port: Int,
    debuggingEnabled: Boolean,
    projectName: String?,
    val packageName: String
) : UnityLocalPlayer(playerId, host, port, debuggingEnabled, projectName) {
    companion object {
        const val TYPE = "UWPPlayer"
    }
}

/**
 * Players that are not local to the current desktop
 *
 * This can include standalone players on other machines, mobile devices, consoles, etc.
 */
class UnityRemotePlayer(playerId: String, host: String, port: Int, debuggingEnabled: Boolean, projectName: String?) :
    UnityProcess(playerId, playerId, host, port, debuggingEnabled, projectName) {

    override fun dump() =
        "$id ($displayName, $host:$port, debugging ${if (debuggingEnabled) "enabled" else "disabled"}, ${projectName ?: "no project name"})"
}

/**
 * User-created player process with hardcoded host and port
 *
 * If the normal player discovery processes fail, the user can enter a custom host and port.
 * We can save the player and reuse the connection details to prevent the need to continually re-enter the details.
 * We can also assume that the custom player is for the current project, or why else would you debug it?
 */
class UnityCustomPlayer(displayName: String, host: String, port: Int, override val projectName: String) :
    UnityProcess("$TYPE($host:$port)", displayName, host, port, true, projectName) {
    companion object {
        const val TYPE = "CustomPlayer"
    }

    override fun dump() = "$id ($displayName, $host:$port, $projectName)"
}

/**
 * Represents a connection to iOS over USB. Does not necessarily mean that a game is running and ready to be debugged.
 *
 * The host and port are hardcoded because we use a proxy (usbmuxd) to forward a local port to the game's remote port
 * (always 56000) over USB.
 * The local port is hardcoded at 12000 based on Unity's own open source debugger plugins.
 */
class UnityIosUsbProcess(displayName: String, val deviceId: String, val deviceDisplayName: String) :
    UnityProcess("$TYPE($deviceId)", displayName, "127.0.0.1", 12000, true) {
    companion object {
        const val TYPE = "iPhoneUSBPlayer"
    }

    override fun dump() = "$id ($displayName, $deviceId, $deviceDisplayName, $host:$port)"
}

/**
 * Represents a Unity game running on Android discovered via ADB, potentially over USB
 *
 * The game is listening on a port on the remote device that is somewhere between 56000 and 57000, inclusive.
 * To connect the debugger, we tell adb to forward the same local port to the remote port over the ADB connection (USB),
 * and connect the debugger to the local port.
 */
class UnityAndroidAdbProcess(
    displayName: String,
    val deviceId: String,
    val deviceDisplayName: String?,
    port: Int,
    val packageUid: String,
    val packageName: String?
) : UnityProcess("$TYPE($deviceId)", displayName, "127.0.0.1", port, true) {
    companion object {
        const val TYPE = "AndroidADBPlayer"
    }

    override val playerInstanceId
        get() = packageName

    override fun dump() =
        "$id ($displayName, $deviceId, $deviceDisplayName, $host:$port, UID: $packageUid, ${packageName ?: "no package name"}"
}
