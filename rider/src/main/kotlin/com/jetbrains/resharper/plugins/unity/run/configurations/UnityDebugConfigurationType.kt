package com.jetbrains.resharper.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.openapi.util.IconLoader

class UnityDebugConfigurationType : ConfigurationTypeBase("UNITY_DEBUG_RUN_CONFIGURATION",
        "Unity Debug", "Attach to Unity process and debug",
        IconLoader.getIcon("/resharper/Logo/UnityLogo.png")) {

    val attachToEditorFactory = UnityAttachToEditorFactory(this)
    // TODO: Add AttachToPlayer factory
    // val attachToPlayerFactory = UnityAttachToPlayerFactory(this)

    init {
        addFactory(attachToEditorFactory)
        // addFactory(attachToPlayerFactory)
    }
}

