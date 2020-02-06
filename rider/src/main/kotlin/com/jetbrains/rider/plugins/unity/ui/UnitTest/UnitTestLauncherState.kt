package com.jetbrains.rider.plugins.unity.ui.UnitTest

import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.StoragePathMacros
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rider.model.UnitTestLaunchPreference
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import org.jdom.Element

@State(name = "UnityUnitTestConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnitTestLauncherState(val project: Project, val propertiesComponent: PropertiesComponent) : PersistentStateComponent<Element> {

    companion object {
        const val discoverLaunchViaUnity = "Discover.Launch.Via.Unity"
        const val currentTestLauncher = "currentTestLauncher"

        private const val NUnit = "NUnit"
        private const val EditMode = "EditMode"
        private const val PlayMode = "PlayMode"
    }

    init {
        if (!propertiesComponent.getBoolean(discoverLaunchViaUnity)) {
            val nestedLifetime = project.lifetime.createNested()
            project.solution.rdUnityModel.sessionInitialized.advise(nestedLifetime){ isConnected ->
                if(isConnected ) {
                    project.solution.rdUnityModel.unitTestPreference.value = UnitTestLaunchPreference.EditMode
                    propertiesComponent.setValue(discoverLaunchViaUnity, true)
                    nestedLifetime.terminate()
                }

            }
        }
    }

    override fun getState(): Element? {
        val element = Element("state")
        val value = getLauncherId(project.solution.rdUnityModel.unitTestPreference.value)
        element.setAttribute(currentTestLauncher, value)
        return element
    }

    override fun loadState(element: Element) {
        val attributeValue = element.getAttributeValue(currentTestLauncher, "") ?: return
        project.solution.rdUnityModel.unitTestPreference.value = getLauncherType(attributeValue)
    }

    private fun getLauncherId(currentPreference: UnitTestLaunchPreference?): String {
        val preferenceNotNull = currentPreference ?: return NUnit

        return when (preferenceNotNull) {
            UnitTestLaunchPreference.EditMode -> EditMode
            UnitTestLaunchPreference.PlayMode -> PlayMode
            UnitTestLaunchPreference.NUnit -> NUnit
        }
    }

    private fun getLauncherType(id: String): UnitTestLaunchPreference {
        when (id) {
            NUnit -> return UnitTestLaunchPreference.NUnit
            EditMode -> return UnitTestLaunchPreference.EditMode
            PlayMode -> return UnitTestLaunchPreference.PlayMode
        }

        return UnitTestLaunchPreference.EditMode
    }

}