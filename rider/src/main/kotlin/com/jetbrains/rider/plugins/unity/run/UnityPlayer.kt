package com.jetbrains.rider.plugins.unity.run

data class UnityPlayer(val host: String, val port: Int, val debuggerPort: Int,
                       val flags: Long, val guid: Long, val editorId: Long, val version: Int,
                       val id: String, val allowDebugging: Boolean, val packageName: String? = null,
                       val projectName: String? = null, val isEditor: Boolean = false) {
    companion object {
        fun createEditorPlayer(host: String, port: Int, id: String, projectName: String?): UnityPlayer {
            return UnityPlayer(host, port, port, flags = 0, guid = port.toLong(), editorId = port.toLong(), version = 0,
                id = id, allowDebugging = true, projectName = projectName, isEditor = true)
        }

        fun createRemotePlayer(host: String, port: Int): UnityPlayer {
            return UnityPlayer(host, port, port, flags = 0, guid = port.toLong(), editorId = port.toLong(), version = 0,
                id = host, allowDebugging = true)
        }
    }
}