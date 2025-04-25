package com.jetbrains.rider.plugins.unity.run.devices

import com.intellij.openapi.actionSystem.AnAction
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.run.devices.DeviceKind
import com.jetbrains.rider.run.devices.NoDeviceAction
import icons.UnityIcons

object UnityDeviceKind : DeviceKind(UnityBundle.message("unity.devices.kind.name"), UnityBundle.message("unity.devices.category")) {
  override fun getMissingDevicesAction(): AnAction {
    return NoDeviceAction(UnityBundle.message("unity.devices.discover.missing.device"), UnityIcons.Toolbar.Companion.ToolbarDisconnected)
  }
}