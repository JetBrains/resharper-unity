package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.*
import com.intellij.openapi.project.Project
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnitTestLaunchPreference
import org.jdom.Element

@Service(Service.Level.PROJECT)
@State(name = "UnityUnitTestConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnitTestLauncherState : PersistentStateComponent<Element> {

    companion object {
        const val currentTestLauncher = "currentTestLauncher"

        private const val NUnit = "NUnit"
        private const val EditMode = "EditMode"
        private const val PlayMode = "PlayMode"
        private const val Both = "Both"

        fun getInstance(project: Project): UnitTestLauncherState = project.service()
    }

    val currentTestLauncherProperty: Property<UnitTestLaunchPreference?> = Property(null) // null means undef

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            getInstance(session.project).currentTestLauncherProperty.flowInto(lifetime, model.unitTestPreference)
            model.unitTestPreference.flowInto(lifetime, getInstance(session.project).currentTestLauncherProperty)

            // initial value, when empty
            if (getInstance(session.project).currentTestLauncherProperty.value == null) {
                val nestedLifetime = lifetime.createNested()
                model.unityEditorConnected.whenTrue(nestedLifetime) {
                    if (getInstance(session.project).currentTestLauncherProperty.value == null) {
                        getInstance(session.project).currentTestLauncherProperty.set(UnitTestLaunchPreference.Both)
                    }
                    nestedLifetime.terminate()
                }
            }
        }
    }

    override fun getState(): Element {
        val element = Element("state")
        val value = currentTestLauncherProperty.value
        if (value != null)
            element.setAttribute(currentTestLauncher, getLauncherId(value))
        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(currentTestLauncher, "")
        if (attributeValue != null && attributeValue.isNotEmpty()) {
            currentTestLauncherProperty.value = getLauncherType(attributeValue)
        }
    }

    private fun getLauncherId(currentPreference: UnitTestLaunchPreference?): String {
        val preferenceNotNull = currentPreference ?: return NUnit

        return when (preferenceNotNull) {
            UnitTestLaunchPreference.EditMode -> EditMode
            UnitTestLaunchPreference.PlayMode -> PlayMode
            UnitTestLaunchPreference.Both -> Both
            UnitTestLaunchPreference.NUnit -> NUnit
        }
    }

    private fun getLauncherType(id: String): UnitTestLaunchPreference {
        when (id) {
            NUnit -> return UnitTestLaunchPreference.NUnit
            EditMode -> return UnitTestLaunchPreference.EditMode
            PlayMode -> return UnitTestLaunchPreference.PlayMode
            Both -> return UnitTestLaunchPreference.Both
        }

        return UnitTestLaunchPreference.Both
    }
}