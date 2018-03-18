package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.jetbrains.rider.plugins.unity.util.UnityIcons

// "UNITY_DEBUG_RUN_CONFIGURATION" is used for a historical reason to avoid undef configuration when updated from old plugin to new one
class UnityDebugConfigurationType : ConfigurationTypeBase("UNITY_DEBUG_RUN_CONFIGURATION",
    "Attach Unity", "Attach to Unity process and debug",
    UnityIcons.Icons.AttachEditorDebugConfiguration) {

    val attachToEditorFactory = UnityAttachToEditorFactory(this)
    // TODO: Add AttachToPlayer factory
    // val attachToPlayerFactory = UnityAttachToPlayerFactory(this)

    init {
        addFactory(attachToEditorFactory)
        // addFactory(attachToPlayerFactory)
    }
}

class UnityDebugAndPlayConfigurationType : ConfigurationTypeBase("UNITY_ATTACH_RUN_CONFIGURATION",
    "Attach Unity and Play", "Attach to UnityEditor and Play",
    UnityIcons.Icons.AttachEditorDebugConfiguration) {

    val attachToEditorAndPlayFactory = UnityAttachToEditorAndPlayFactory(this)

    init {
        addFactory(attachToEditorAndPlayFactory)
    }
}


