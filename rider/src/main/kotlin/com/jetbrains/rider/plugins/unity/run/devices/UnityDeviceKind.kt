package com.jetbrains.rider.plugins.unity.run.devices

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.run.devices.DeviceKind
import org.jetbrains.annotations.Nls

open class UnityDeviceKind(id: String, @Nls name: String) : DeviceKind(id, name, name) {

  override fun getMissingDevicesAction(project: Project): AnAction? {
    return null
  }
}

object UnityUsbDeviceKind : UnityDeviceKind("unity_usb_device", UnityBundle.message("project.name.usb.devices")) {
}

object UnityCustomPlayerDeviceKind : UnityDeviceKind("unity_custom_player", UnityBundle.message("unity.custom.devices.kind.name")) {
}

object UnityRemotePlayerDeviceKind : UnityDeviceKind("unity_remote_player", UnityBundle.message("unity.remote.devices.kind.name")) {
}

object UnityLocalPlayerDeviceKind : UnityDeviceKind("unity_local_player", UnityBundle.message("unity.local.devices.kind.name")) {
}

object UnityLocalUwpPlayerDeviceKind : UnityDeviceKind("unity_local_uwp_player", UnityBundle.message("unity.local.uwp.devices.kind.name")) {
}

object UnityEditorDeviceKind : UnityDeviceKind("unity_editor", UnityBundle.message("unity.editor.devices.kind.name")) {
}

object UnityVirtualPlayerDeviceKind : UnityDeviceKind("unity_virtual_player", UnityBundle.message("unity.virtual.player.devices.kind.name")) {
}