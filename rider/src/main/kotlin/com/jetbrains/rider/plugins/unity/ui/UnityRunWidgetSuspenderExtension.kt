package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.IOptPropertyView
import com.jetbrains.rd.util.reactive.OptProperty
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.ideaInterop.toolbar.RunWidgetSuspenderExtension;
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class UnityRunWidgetSuspenderExtension(private val project: Project) : RunWidgetSuspenderExtension {
    private var isRunWidgetAllowedProp = OptProperty<Boolean>()

    override val isRunWidgetAllowed: IOptPropertyView<Boolean> = isRunWidgetAllowedProp

    init {
        val isUnity = UnityProjectDiscoverer.getInstance(project).isUnityProject
        isRunWidgetAllowedProp.set(!isUnity)
        if (!isUnity) {
            project.solution.frontendBackendModel.hasUnityReference.advise(project.lifetime) {
                isRunWidgetAllowedProp.set(!it)
            }
        }
    }
}