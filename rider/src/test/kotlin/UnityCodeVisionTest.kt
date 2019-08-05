import com.jetbrains.rider.test.base.CodeLensBaseTest

class UnityCodeVisionTest : CodeLensBaseTest() {

    companion object {
        const val assetUsagesProvider = "Unity Assets Usage"
        const val unityFieldProvider = "Unity serialized field"
        const val impicitUsagesProvider = "Unity implicit usage"
    }

    override val waitForCaches = true

    override fun getSolutionDirectoryName() = "CodeLensTestSolution"
}