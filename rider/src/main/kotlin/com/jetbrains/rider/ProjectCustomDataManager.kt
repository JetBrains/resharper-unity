package com.jetbrains.rider

import com.intellij.ide.impl.ProjectUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.ILifetimedComponent
import com.jetbrains.rider.util.idea.LifetimedComponent

class ProjectCustomDataManager(val project: Project) : ILifetimedComponent by LifetimedComponent(project) {
    val logger = Logger.getInstance(ProjectCustomDataManager::class.java)
    init {
        project.solution.customData.data.advise(componentLifetime) { item ->
            if (item.key == "UNITY_ActivateRider" && item.newValueOpt == "true") {
                logger.info(item.key+" "+ item.newValueOpt)
                ProjectUtil.focusProjectWindow(project, true)
                project.solution.customData.data["UNITY_ActivateRider"] = "false";
            }
        }
    }
}