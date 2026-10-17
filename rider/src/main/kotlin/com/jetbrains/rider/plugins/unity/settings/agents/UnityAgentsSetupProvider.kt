package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import org.jetbrains.annotations.ApiStatus

/** The three steps of the Unity for Agents setup, in the order the setup chain runs them. */
@ApiStatus.Internal
enum class UnityAgentsSetupStep {
    CLI,
    PIPELINE_PACKAGE,
    CLI_SKILL,
}

@ApiStatus.Internal
interface UnityAgentsSetupProvider {
    /** False when the setup does not apply: the feature is off, the project is default, not a Unity project, or not trusted. */
    fun isAvailable(): Boolean

    /**
     * Reads the steps that are not done yet, in the order of [UnityAgentsSetupStep].
     * Returns null when the setup is not [isAvailable].
     * Runs the Unity CLI up to three times.
     */
    suspend fun readMissingSteps(): List<UnityAgentsSetupStep>?

    /** True while a setup chain runs. */
    fun isRunning(): Boolean

    /** Runs the setup chain and returns when it ends. A call while a chain runs waits for that chain and starts no new one. */
    suspend fun setup()

    /** Returns when the running setup chain ends. Starts no chain. */
    suspend fun awaitSetup()

    companion object {
        fun getInstance(project: Project): UnityAgentsSetupProvider = project.service<UnityAgentsSetupService>()
    }
}
