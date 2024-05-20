package com.jetbrains.rider.unity.test.framework

import com.jetbrains.rider.test.framework.TestMethod

@Target(AnnotationTarget.FUNCTION)
annotation class UnityTestEnvironment(
    val withCoverage: Boolean = false,
    val resetEditorPrefs: Boolean = false,
    val useRiderTestPath: Boolean = false,
    val batchMode: Boolean = true
)

class UnityTestEnvironmentInstance(
    val withCoverage: Boolean = false,
    val resetEditorPrefs: Boolean = false,
    val useRiderTestPath: Boolean = false,
    val batchMode: Boolean = true
) {
    companion object {
        fun getFromAnnotation(unityTestEnvironment: UnityTestEnvironment?): UnityTestEnvironmentInstance? {
            if (unityTestEnvironment == null) {
                return null
            }

            return UnityTestEnvironmentInstance(
                unityTestEnvironment.withCoverage,
                unityTestEnvironment.resetEditorPrefs,
                unityTestEnvironment.useRiderTestPath,
                unityTestEnvironment.batchMode
            )
        }
    }
}

val TestMethod.unityEnvironment: UnityTestEnvironmentInstance?
    get() = UnityTestEnvironmentInstance.getFromAnnotation(
        method.annotations.filterIsInstance<UnityTestEnvironment>().firstOrNull())

enum class UnityVersion(val version: String) {
    V2020("2020"),
    V2022("2022"),
    V2023("2023"),
    V6("6000")
}