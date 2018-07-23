package com.jetbrains.rider.plugins.unity.run.attach

data class UnityPlayer(val host: String, val port: Int, val debuggerPort: Int,
                       val flags: Long, val guid: Long, val editorId: Long, val version: Int,
                       val id: String, val allowDebugging: Boolean, val isEditor: Boolean)