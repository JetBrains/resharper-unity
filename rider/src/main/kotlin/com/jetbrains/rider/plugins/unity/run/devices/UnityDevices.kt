package com.jetbrains.rider.plugins.unity.run.devices

import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.plugins.unity.run.UnityAndroidAdbPlayer
import com.jetbrains.rider.plugins.unity.run.UnityCustomPlayer
import com.jetbrains.rider.plugins.unity.run.UnityDebugEngine
import com.jetbrains.rider.plugins.unity.run.UnityDebugTarget
import com.jetbrains.rider.plugins.unity.run.UnityEditor
import com.jetbrains.rider.plugins.unity.run.UnityEditorHelper
import com.jetbrains.rider.plugins.unity.run.UnityIosUsbPlayer
import com.jetbrains.rider.plugins.unity.run.UnityLocalPlayer
import com.jetbrains.rider.plugins.unity.run.UnityLocalUwpPlayer
import com.jetbrains.rider.plugins.unity.run.UnityProcessPickerDialog
import com.jetbrains.rider.plugins.unity.run.UnityRemotePlayer
import com.jetbrains.rider.plugins.unity.run.UnityVirtualPlayer
import com.jetbrains.rider.run.devices.Device
import icons.UnityIcons

/**
 * Used to connect to the current project's editor without needing current connection details
 */
class UnityCurrentProjectEditorDevice(name: String, val playOnConnect: Boolean)
    : Device(name, UnityIcons.RunConfigurations.AttachToPlayer, UnityEditorDeviceKind)

/**
 * Used to connect to a Unity debug target such as a local process, or local/remote player
 */
class UnityProcessDevice(val process: UnityDebugTarget, name: String, kind: UnityDeviceKind)
    : Device(name, process.icon, kind)


@NlsSafe
private fun formatDeviceName(projectName: String?, displayName: String, debugEngine: UnityDebugEngine): String {
    return when {
        displayName.isNotEmpty() -> displayName
        !projectName.isNullOrEmpty() && projectName != UnityProcessPickerDialog.CUSTOM_PLAYER_PROJECT -> "$projectName (${debugEngine.toPresentableString()})"
        else -> debugEngine.toPresentableString()
    }
}

fun UnityDebugTarget.toDevice(): UnityProcessDevice {
    val deviceName = formatDeviceName(projectName, name, debugEngine)
    return UnityProcessDevice(this, deviceName, deviceKind)
}

val UnityDebugTarget.deviceKind: UnityDeviceKind
    get() = when (this) {
        is UnityAndroidAdbPlayer -> UnityUsbDeviceKind
        is UnityCustomPlayer -> UnityCustomPlayerDeviceKind
        is UnityIosUsbPlayer -> UnityUsbDeviceKind
        is UnityLocalUwpPlayer -> UnityLocalUwpPlayerDeviceKind
        is UnityLocalPlayer -> UnityLocalPlayerDeviceKind
        is UnityRemotePlayer -> UnityRemotePlayerDeviceKind
        is UnityEditor -> UnityEditorDeviceKind
        is UnityEditorHelper -> UnityEditorDeviceKind
        is UnityVirtualPlayer -> UnityVirtualPlayerDeviceKind
    }
