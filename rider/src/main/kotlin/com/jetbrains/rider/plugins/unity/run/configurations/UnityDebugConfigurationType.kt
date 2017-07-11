package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.jetbrains.rider.plugins.unity.util.UnityIcons

class UnityDebugConfigurationType : ConfigurationTypeBase("UNITY_DEBUG_RUN_CONFIGURATION",
        "Unity Debug", "Attach to Unity process and debug",
        UnityIcons.AttachEditorDebugConfiguration) {

    val attachToEditorFactory = UnityAttachToEditorFactory(this)
    // TODO: Add AttachToPlayer factory
    // val attachToPlayerFactory = UnityAttachToPlayerFactory(this)

    init {
        addFactory(attachToEditorFactory)
        // addFactory(attachToPlayerFactory)
    }
}

