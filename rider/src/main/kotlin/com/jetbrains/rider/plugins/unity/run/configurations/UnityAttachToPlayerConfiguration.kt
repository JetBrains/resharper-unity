package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.icons.AllIcons
import com.jetbrains.rider.debugger.IDotNetDebuggable
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import javax.swing.Icon

class UnityAttachToPlayerConfiguration(debuggerPort : Int, host: String = "127.0.0.1") : RemoteConfiguration, IDotNetDebuggable {
    override var port: Int = debuggerPort
    override var address: String = host
    override var listenPortForConnections: Boolean = false
}