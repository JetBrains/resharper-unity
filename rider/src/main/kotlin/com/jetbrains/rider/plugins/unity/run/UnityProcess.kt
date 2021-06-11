package com.jetbrains.rider.plugins.unity.run

import com.jetbrains.rider.plugins.unity.util.convertPidToDebuggerPort

sealed class UnityProcess(val displayName: String, val allowDebugging: Boolean, val projectName: String? = null)

interface UnityRemoteConnectionDetails {
    val host: String
    val port: Int
}

sealed class UnityLocalProcess(name: String, val pid: Int, projectName: String?) : UnityProcess(name, true, projectName), UnityRemoteConnectionDetails {
    override val host = "127.0.0.1"
    override val port = convertPidToDebuggerPort(pid)
}

class UnityEditor(displayName: String, pid: Int, projectName: String?): UnityLocalProcess(displayName, pid, projectName)
class UnityEditorHelper(displayName: String, val roleName: String, pid: Int, projectName: String?): UnityLocalProcess(displayName, pid, projectName)

// TODO: If we know it's a local player, can we get rid of the host address and just use 127.0.0.1?
// Could that fail with multiple local addresses? Is that even a thing?
open class UnityLocalPlayer(displayName: String, override val host: String, override val port: Int, allowDebugging: Boolean, projectName: String?)
    : UnityProcess(displayName, allowDebugging, projectName), UnityRemoteConnectionDetails
class UnityRemotePlayer(displayName: String, override val host: String, override val port: Int, allowDebugging: Boolean, projectName: String?)
    : UnityProcess(displayName, allowDebugging, projectName), UnityRemoteConnectionDetails

class UnityLocalUwpPlayer(displayName: String, override val host: String, override val port: Int, allowDebugging: Boolean, projectName: String?, val packageName: String)
    : UnityLocalPlayer(displayName, host, port, allowDebugging, projectName)

class UnityIosUsbProcess(displayName: String, val deviceId: String) : UnityProcess(displayName, true)
