package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.jetbrains.rider.icons.ReSharperCommonIcons
import com.jetbrains.rider.plugins.unity.RdLogEventMode
import com.jetbrains.rider.plugins.unity.RdLogEventType
import com.jetbrains.rider.plugins.unity.util.UnityIcons

object UnityLogPanelIcons {
    val Error = ReSharperCommonIcons.Error
    val Warning = ReSharperCommonIcons.Warning
    val Message = ReSharperCommonIcons.MessageInfo

    val Edit = ReSharperCommonIcons.Edit
    val Play = UnityIcons.Toolwindows.ToolWindowUnityLog
}

fun RdLogEventType.getIcon() = when (this) {
    RdLogEventType.Error -> UnityLogPanelIcons.Error
    RdLogEventType.Warning -> UnityLogPanelIcons.Warning
    RdLogEventType.Message -> UnityLogPanelIcons.Message
}

fun RdLogEventMode.getIcon() = when (this) {
    RdLogEventMode.Edit -> UnityLogPanelIcons.Edit
    RdLogEventMode.Play -> UnityLogPanelIcons.Play
}