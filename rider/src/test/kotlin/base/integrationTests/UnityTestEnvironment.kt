package base.integrationTests

import com.jetbrains.rider.test.framework.TestMethod

@Target(AnnotationTarget.FUNCTION)
annotation class UnityTestEnvironment(
    val resetEditorPrefs: Boolean = false,
    val useRiderTestPath: Boolean = false,
    val batchMode: Boolean = true
)

class UnityTestEnvironmentInstance(
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