package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.jetbrains.rider.plugins.unity.util.UnityIcons

class UnityDebugConfigurationType : ConfigurationTypeBase("UNITY_DEBUG_CONFIGURATION",
        "Attach Unity", "Attach to Unity process and debug",
        UnityIcons.AttachEditorDebugConfiguration) {

    val attachToEditorFactory = UnityAttachToEditorFactory(this)
    // TODO: Add AttachToPlayer factory
    // val attachToPlayerFactory = UnityAttachToPlayerFactory(this)

    init {
        addFactory(attachToEditorFactory)
        // addFactory(attachToPlayerFactory)
    }
}

class UnityDebugAndPlayConfigurationType : ConfigurationTypeBase("UNITY_DEBUG_RUN_CONFIGURATION",
    "Attach Unity and Play", "Attach to UnityEditor and Play",
    UnityIcons.AttachEditorDebugConfiguration) {

    val attachToEditorAndPlayFactory = UnityAttachToEditorAndPlayFactory(this)

    init {
        addFactory(attachToEditorAndPlayFactory)
    }
}


