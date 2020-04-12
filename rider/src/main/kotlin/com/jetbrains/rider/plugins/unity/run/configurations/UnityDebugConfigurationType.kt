package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationTypeBase
import icons.UnityIcons

// We need to keep "UNITY_DEBUG_RUN_CONFIGURATION" for backwards compatibility - a user can run a newer EAP side by side
// with 2018.1 and still load the standard attach/debug run config. The new attach/debug/play config will still come up
// as "Unknown", but we can't help that
class UnityDebugConfigurationType : ConfigurationTypeBase(id,
    "Attach to Unity Editor", "Attach to Unity process and debug",
    UnityIcons.RunConfigurations.AttachToUnityParentConfiguration) {

    val attachToEditorFactory = UnityAttachToEditorFactory(this)
    val attachToEditorAndPlayFactory = UnityAttachToEditorAndPlayFactory(this)

    init {
        addFactory(attachToEditorFactory)
        addFactory(attachToEditorAndPlayFactory)
    }

    companion object {
        val id = "UNITY_DEBUG_RUN_CONFIGURATION"
    }
}
