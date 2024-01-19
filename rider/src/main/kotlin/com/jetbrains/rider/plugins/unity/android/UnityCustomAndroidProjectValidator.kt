package com.jetbrains.rider.plugins.unity.android

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.getCompletedOr
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.run.multiPlatform.android.ICustomAndroidProjectValidator

class UnityCustomAndroidProjectValidator : ICustomAndroidProjectValidator {
    override fun isAndroidProject(project: Project): Boolean = project.isUnityProject.getCompletedOr(false)
}