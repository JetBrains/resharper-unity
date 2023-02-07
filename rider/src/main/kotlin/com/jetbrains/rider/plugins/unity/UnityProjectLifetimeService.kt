package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.Disposable
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition

/**
 * Simple service to act as a parent disposable for the Unity plugin
 *
 * It is a bad practice for a plugin to use [Project] as a parent disposable. This service will be disposed when the
 * project is closed, or when/if the plugin is unloaded. It should be used as a parent disposable, or to provide access
 * to its [Lifetime], or to create a nested [LifetimeDefinition].
 */
class UnityProjectLifetimeService: LifetimedService(), Disposable {
    companion object {
        fun getInstance(project: Project): UnityProjectLifetimeService = project.service()
        fun getLifetime(project: Project) = getInstance(project).serviceLifetime
        fun getNestedLifetimeDefinition(project: Project) = getLifetime(project).createNested()
    }
}