package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.jetbrains.rider.plugins.unity.model.LogEventMode
import com.jetbrains.rider.plugins.unity.model.LogEventType
import UnityIcons

fun LogEventType.getIcon() = when (this) {
    LogEventType.Error -> UnityIcons.Ide.Error
    LogEventType.Warning -> UnityIcons.Ide.Warning
    LogEventType.Message -> UnityIcons.Ide.Info
}

fun LogEventMode.getIcon() = when (this) {
    LogEventMode.Edit -> UnityIcons.Actions.FilterEditModeMessages
    LogEventMode.Play -> UnityIcons.Actions.FilterPlayModeMessages
}