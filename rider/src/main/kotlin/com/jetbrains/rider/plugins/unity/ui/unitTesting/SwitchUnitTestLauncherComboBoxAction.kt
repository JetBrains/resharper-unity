package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ex.ComboBoxAction
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.model.UnitTestLaunchPreference
import com.jetbrains.rider.model.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import javax.swing.JComponent

class SwitchUnitTestLauncherComboBoxAction : ComboBoxAction() {

    private fun getLauncherDescription(currentPreference: UnitTestLaunchPreference?): String {
        val preferenceNotNull = currentPreference ?: return UseNunitLauncherAction.NUnitDescription

        return when (preferenceNotNull) {
            UnitTestLaunchPreference.EditMode -> UseUnityEditLauncherAction.EditModeDescription
            UnitTestLaunchPreference.NUnit -> UseNunitLauncherAction.NUnitDescription
            UnitTestLaunchPreference.PlayMode -> UseUnityPlayLauncherAction.PlayModeDescription
        }
    }

    override fun createPopupActionGroup(p0: JComponent?): DefaultActionGroup {
        return object : DefaultActionGroup(UseUnityEditLauncherAction(), UseUnityPlayLauncherAction(), UseNunitLauncherAction()) {
            override fun update(e: AnActionEvent) {
                val project = e.project ?: return

                val currentPreference = project.solution.frontendBackendModel.unitTestPreference.value
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
    }

    override fun update(e: AnActionEvent) {

        val project = e.project ?: return
        val currentPreference = project.solution.frontendBackendModel.unitTestPreference.value
        e.presentation.text = getLauncherDescription(currentPreference)

        e.presentation.description = getLauncherDescription(currentPreference)
        e.presentation.isEnabledAndVisible = project.isUnityProject()

        super.update(e)
    }

    override fun shouldShowDisabledActions(): Boolean {
        return true
    }
}