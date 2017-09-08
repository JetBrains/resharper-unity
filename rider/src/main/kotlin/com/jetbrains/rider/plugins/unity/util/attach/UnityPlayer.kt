package com.jetbrains.rider.plugins.unity.util.attach

data class UnityPlayer(val host: String, val port: Int, val flags: Long,
                       val guid: Long, val editorId: Long, val version: Int,
                       val id: String, val allowDebugging: Boolean, val debuggerPort: Int)