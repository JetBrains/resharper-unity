package com.jetbrains.rider.plugins.unity.android

import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.run.xamarin.android.ICustomAndroidProjectValidator

class UnityCustomAndroidProjectValidator : ICustomAndroidProjectValidator {
    override fun isAndroidProject(project: Project): Boolean = UnityProjectDiscoverer.getInstance(project).isUnityProject
}