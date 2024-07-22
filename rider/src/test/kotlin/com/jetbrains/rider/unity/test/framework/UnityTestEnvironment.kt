package com.jetbrains.rider.unity.test.framework

import com.jetbrains.rider.test.framework.TestMethod

//Update this version when the new one is available
const val riderPackageVersion: String = "3.0.31"

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

interface EngineVersion {
    val version: String
    fun isTuanjie(): Boolean
    fun isUnity(): Boolean
}

enum class Tuanjie(override val version: String) : EngineVersion {
    V2022("2022");

    override fun isTuanjie(): Boolean = true
    override fun isUnity(): Boolean = false
}

enum class Unity(override val version: String) : EngineVersion {
    V2020("2020"),
    V2022("2022"),
    V2023("2023"),
    V6("6000");

    override fun isTuanjie(): Boolean = false
    override fun isUnity(): Boolean = true
}