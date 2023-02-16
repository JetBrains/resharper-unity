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
 */
sealed class UnityProcess(@NlsSafe val displayName: String,
                          @NlsSafe val host: String,
                          val port: Int,
                          val debuggingEnabled: Boolean,
                          @NlsSafe open val projectName: String? = null)

/** Base class of Unity players which are also local processes, such as the Editor */
sealed class UnityLocalProcess(name: String, val pid: Int, projectName: String?)
    : UnityProcess(name, "127.0.0.1", convertPidToDebuggerPort(pid), true, projectName)

/**
 * A Unity editor instance
 *
 * The user might have multiple of these.
 * The project name can help disambiguate, and the current project's editor will save the process ID into the project's
 * `Library/EditorInstance.json` file.
 */
class UnityEditor(displayName: String, pid: Int, projectName: String?): UnityLocalProcess(displayName, pid, projectName)

/**
 * A helper process for an editor, such as an asset importer
 *
 * The display name is the helper's role, e.g. "AssetImportWorker0", and the project name will be used to group with its
 * parent editor.
 * The project name should always be available, but this is not guaranteed.
 */
class UnityEditorHelper(displayName: String, @NlsSafe val roleName: String, pid: Int, projectName: String?): UnityLocalProcess(displayName, pid, projectName)

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
open class UnityLocalPlayer(displayName: String, host: String, port: Int, debuggingEnabled: Boolean, projectName: String?)
    : UnityProcess(displayName, host, port, debuggingEnabled, projectName)

/**
 * A local Windows UWP player
 *
 * UWP processes require special handling for debugging, so need to be recognised separately.
 * Note that we don't hardcode the host, because it can still be a local address even if it's not "127.0.0.1", and using
 * the value that we're given has the best chance of working with the debugger.
 */
class UnityLocalUwpPlayer(displayName: String, host: String, port: Int, debuggingEnabled: Boolean, projectName: String?, val packageName: String)
    : UnityLocalPlayer(displayName, host, port, debuggingEnabled, projectName)

/**
 * Players that are not local to the current desktop
 *
 * This can include standalone players on other machines, mobile devices, consoles, etc.
 */
class UnityRemotePlayer(displayName: String, host: String, port: Int, debuggingEnabled: Boolean, projectName: String?)
    : UnityProcess(displayName, host, port, debuggingEnabled, projectName)

/**
 * Represents a connection to iOS over USB. Does not necessarily mean that a game is running and ready to be debugged.
 *
 * The host and port are hardcoded because we use a proxy (usbmuxd) to forward a local port to the game's remote port
 * (always 56000) over USB.
 * The local port is hardcoded at 12000 based on Unity's own open source debugger plugins.
 */
class UnityIosUsbProcess(displayName: String, val deviceId: String)
    : UnityProcess(displayName, "127.0.0.1", 12000, true)

/**
 * Represents a Unity game running on Android discovered via ADB, potentially over USB
 *
 * The game is listening on a port on the remote device that is somewhere between 56000 and 57000, inclusive.
 * To connect the debugger, we tell adb to forward the same local port to the remote port over the ADB connection (USB),
 * and connect the debugger to the local port.
 */
class UnityAndroidAdbProcess(displayName: String, val deviceId: String, val deviceDisplayName: String?, port: Int, val packageName: String?)
    : UnityProcess(displayName, "127.0.0.1", port, true)
