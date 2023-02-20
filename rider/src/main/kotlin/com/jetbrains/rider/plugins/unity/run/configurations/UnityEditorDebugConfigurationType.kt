package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.execution.configurations.VirtualConfigurationType
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

class UnityEditorDebugConfigurationType : ConfigurationTypeBase(
    id,
    UnityBundle.message("configuration.type.name.attach.to.unity.editor"),
    UnityBundle.message("configuration.type.description.attach.to.unity.process.and.debug"),
    UnityIcons.RunConfigurations.AttachToUnityParentConfiguration
), VirtualConfigurationType, DumbAware {

    val attachToEditorFactory = UnityAttachToEditorFactory(this)
    val attachToEditorAndPlayFactory = UnityAttachToEditorAndPlayFactory(this)

    init {
        addFactory(attachToEditorFactory)
        addFactory(attachToEditorAndPlayFactory)
    }

    companion object {
        // Note that this value is incorrect. The JavaDoc states that the ID should be camel cased without dashes or
        // underscores, etc. But it's used as the key for persisting run configuration settings, and so shouldn't ever
        // be changed. Too late now.
        const val id = "UNITY_DEBUG_RUN_CONFIGURATION"
    }
}
