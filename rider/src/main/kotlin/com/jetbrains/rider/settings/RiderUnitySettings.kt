package com.jetbrains.rider.settings

import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Signal
import com.jetbrains.rdclient.util.idea.ProtocolSubscribedProjectComponent

class RiderUnitySettings(project: Project) : ProtocolSubscribedProjectComponent(project) {
    companion object {
        private val properties = PropertiesComponent.getInstance()
        private const val propertiesPrefix = "RiderUnitySettings."
    }

    class BooleanViewProperty(val name: String, private val defaultValue: Boolean = false) {
        val update = Signal<Boolean>()

        fun advise(lifetime: Lifetime, handler: (Boolean) -> Unit) = update.advise(lifetime, handler)

        var value: Boolean
            get() = properties.getBoolean(propertiesPrefix + name, defaultValue)
            set(newValue) {
                val oldValue = properties.getBoolean(propertiesPrefix + name, defaultValue)
                if (oldValue != newValue) {
                    properties.setValue(propertiesPrefix + name, newValue)
                    update.fire(value)
                }
            }

        fun invert() {
            value = !value
        }
    }
}