package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.jetbrains.rider.icons.ReSharperCommonIcons
import com.jetbrains.rider.plugins.unity.editorPlugin.model.*
import com.jetbrains.rider.plugins.unity.util.UnityIcons

object UnityLogPanelIcons {
    val Error = UnityIcons.Ide.Error
    val Warning = ReSharperCommonIcons.Warning
    val Message = UnityIcons.Ide.Info

    val Edit = UnityIcons.Unity.UnityEdit
    val Play = UnityIcons.Unity.UnityPlay
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