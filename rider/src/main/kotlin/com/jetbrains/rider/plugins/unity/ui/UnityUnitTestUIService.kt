package com.jetbrains.rider.plugins.unity.ui

import com.intellij.icons.AllIcons
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ex.ComboBoxAction
import com.intellij.openapi.components.PersistentStateComponent
import com.intellij.openapi.components.State
import com.intellij.openapi.components.Storage
import com.intellij.openapi.components.StoragePathMacros
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.jetbrains.rider.isConnectedToEditor
import com.jetbrains.rider.isConnectedToEditorLive
import com.jetbrains.rider.isUnityGeneratedProject
import com.jetbrains.rider.model.UnitTestLaunchPreference
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.unitTesting.RiderUnitTestUIService
import org.jdom.Element
import javax.swing.JComponent


@State(name = "UnityUnitTestConfiguration", storages = [(Storage(StoragePathMacros.WORKSPACE_FILE))])
class UnityUnitTestUIService(project: Project, val propertiesComponent: PropertiesComponent) : RiderUnitTestUIService(project), PersistentStateComponent<Element> {
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

    companion object {
        const val discoverLaunchViaUnity = "Discover.Launch.Via.Unity"

        const val currentTestLauncher = "currentTestLauncher"


        private val NUnit = "NUnit"
        private val NUnitDescription = "Standalone NUnit Launcher"
        private val EditMode = "EditMode"
        private val EditModeDescription = "Unity Editor - Edit Mode"
    }

    override fun customizeTopToolBarActionGroup(actionGroup: DefaultActionGroup) {
        if (project.isUnityGeneratedProject()) {
            actionGroup.addSeparator()
            actionGroup.add(switchUnitTestLauncherComboBox)

            //advertise launching via Unity Editor for the very first time
            if (!propertiesComponent.getBoolean(discoverLaunchViaUnity)) {
                project.isConnectedToEditorLive().advise(componentLifetime){ isConnected ->
                    if(isConnected)
                        project.solution.rdUnityModel.unitTestPreference.value = UnitTestLaunchPreference.EditMode
                }
                propertiesComponent.setValue(discoverLaunchViaUnity, true)
            }
        }
    }

    private fun getLauncherId(currentPreference: UnitTestLaunchPreference?): String {
        val preferenceNotNull = currentPreference ?: return NUnit

        when (preferenceNotNull) {
            UnitTestLaunchPreference.EditMode -> return EditMode
            UnitTestLaunchPreference.NUnit -> return NUnit
        }
    }

    private fun getLauncherType(id: String): UnitTestLaunchPreference {
        when (id) {
            NUnit -> return UnitTestLaunchPreference.NUnit
            EditMode -> return UnitTestLaunchPreference.EditMode
        }

        return UnitTestLaunchPreference.EditMode
    }

    private fun getLauncherDescription(currentPreference: UnitTestLaunchPreference?): String {
        val preferenceNotNull = currentPreference ?: return NUnitDescription

        when (preferenceNotNull) {
            UnitTestLaunchPreference.EditMode -> return EditModeDescription
            UnitTestLaunchPreference.NUnit -> return NUnitDescription
        }
    }

    val useNunitLauncher = object : DumbAwareAction(NUnitDescription, "Run with NUnit launcher", null) {
        override fun actionPerformed(p0: AnActionEvent?) {
            project.solution.rdUnityModel.unitTestPreference.value = UnitTestLaunchPreference.NUnit
        }
    }

    val useUnityEditLauncher = object : DumbAwareAction(EditModeDescription, "Run with Unity Editor in Edit Mode", null) {
        override fun actionPerformed(p0: AnActionEvent?) {
            project.solution.rdUnityModel.unitTestPreference.value = UnitTestLaunchPreference.EditMode
        }

        override fun update(e: AnActionEvent) {
            e.presentation.isEnabled = project.isConnectedToEditor()
            e.presentation.isVisible = true
        }
    }

    val switchUnitTestLauncherGroup = object : DefaultActionGroup(useUnityEditLauncher, useNunitLauncher) {
        override fun update(e: AnActionEvent) {

            val currentPreference = project.solution.rdUnityModel.unitTestPreference.value
            e.presentation.text = getLauncherDescription(currentPreference)

            e.presentation.description = getLauncherDescription(currentPreference)
            e.presentation.isEnabledAndVisible = true
            e.presentation.icon = AllIcons.General.Settings

            super.update(e)
        }

        override fun isPopup(): Boolean {
            return true
        }
    }

    private val switchUnitTestLauncherComboBox = object : ComboBoxAction() {
        override fun createPopupActionGroup(p0: JComponent?): DefaultActionGroup {
            return switchUnitTestLauncherGroup
        }

        override fun update(e: AnActionEvent) {

            val currentPreference = project.solution.rdUnityModel.unitTestPreference.value
            e.presentation.text = getLauncherDescription(currentPreference)

            e.presentation.description = getLauncherDescription(currentPreference)
            e.presentation.isEnabledAndVisible = true

            super.update(e)
        }

        override fun shouldShowDisabledActions(): Boolean {
            return true
        }
    }

}