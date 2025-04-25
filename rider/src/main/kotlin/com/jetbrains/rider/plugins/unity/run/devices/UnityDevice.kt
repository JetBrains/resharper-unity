package com.jetbrains.rider.plugins.unity.run.devices

import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.plugins.unity.run.UnityProcess
import com.jetbrains.rider.run.devices.Device
import icons.UnityIcons

class UnityDevice(val process: UnityProcess) : Device(
    formatDeviceName(process.displayName, process.host, process.port),
    UnityIcons.RunConfigurations.AttachToPlayer,
    UnityDeviceKind
) {
    companion object
}

// also RIDER-125170 ActiveDevice-presentation-in-the-toolbar

@NlsSafe
fun formatDeviceName(name: String, ip: String, port:Int) = if (name.isEmpty()) "$ip:$port" else "$name ($ip:$port)"

fun UnityProcess.toUnityDevice(): UnityDevice {
    return UnityDevice(this)
}