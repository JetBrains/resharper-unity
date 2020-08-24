package integrationTests

import base.integrationTests.*
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import base.integrationTests.waitForUnityEditorPlaying as waitForUnityEditorPlaying1

class DebuggerTest : IntegrationDebuggerTestBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithoutPlugin"

    @Test
    fun checkUnityPausePoint() {
        selectAttachDebuggerToUnityEditorAndPlayConfiguration()

        val pauseFile = activeSolutionDirectory.combine("Assets", "pause").absoluteFile
        debugUnityProgramWithoutGold(
            {
                toggleUnityPausepoint("NewBehaviourScript.cs", 17, "System.IO.File.Exists(\"${pauseFile.path}\")")
            }
        ) {
            waitForUnityEditorPlaying1()
            pauseFile.createNewFile()
            waitForUnityEditorPaused()
            unpause()
            waitForUnityEditorPlaying1()
        }
    }

    @Test
    fun checkBreakpoint() {
        selectAttachDebuggerToUnityEditorAndPlayConfiguration()

        debugUnityProgramWithGold(testGoldFile,
            {
                toggleBreakpoint("NewBehaviourScript.cs", 18)
            }
        ) {
            waitForPause()
            dumpFullCurrentData()
            resumeSession()
        }
    }
}