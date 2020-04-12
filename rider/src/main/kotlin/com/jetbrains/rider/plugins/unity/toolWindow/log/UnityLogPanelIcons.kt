package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import icons.UnityIcons

fun RdLogEventType.getIcon() = when (this) {
    RdLogEventType.Error -> UnityIcons.Ide.Error
    RdLogEventType.Warning -> UnityIcons.Ide.Warning
    RdLogEventType.Message -> UnityIcons.Ide.Info
}

fun RdLogEventMode.getIcon() = when (this) {
    RdLogEventMode.Edit -> UnityIcons.Actions.FilterEditModeMessages
    RdLogEventMode.Play -> UnityIcons.Actions.FilterPlayModeMessages
}