package com.jetbrains.rider.plugins.unity.settings

import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.components.Service
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Signal

// todo: it has no init {}, stores no state apart from properties = PropertiesComponent.getInstance() which is also bad.
// There's no need for this class, it has no payload BooleanViewProperty does.
// BooleanViewProperty can be renamed to something matching its meaning like PropertiesBackedViewableBooleanSetting. But I'd not recommend using this as a pattern. As it doesn't react to changes in PropertiesComponent or external modifications in xml PropertiesComponent uses for storage.
@Service(Service.Level.PROJECT)
class RiderUnitySettings : LifetimedService() {
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