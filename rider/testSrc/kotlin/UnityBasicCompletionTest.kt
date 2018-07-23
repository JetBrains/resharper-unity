import com.intellij.openapi.editor.impl.EditorImpl
import com.jetbrains.rider.test.base.CompletionTestBase
import com.jetbrains.rider.test.scriptingApi.completeWithTab
import com.jetbrains.rider.test.scriptingApi.typeWithLatency
import com.jetbrains.rider.test.scriptingApi.waitForCompletion
import org.testng.annotations.Test

@Test
class UnityBasicCompletionTest : CompletionTestBase() {
    override fun getSolutionDirectoryName() = "BasicUnityProject"
    override val restoreNuGetPackages = true

    @Test
    fun basicCompletion() {
        doTest {
            typeWithLatency("pr")
            waitForCompletion()
            completeWithTab()
        }
    }

    private fun doTest(test: EditorImpl.() -> Unit) {
// TODO: make tests run on CI
//        withCaret("Assets\\NewSurfaceShader.shader", "NewSurfaceShader.shader") {
//            test()
//        }
    }
}