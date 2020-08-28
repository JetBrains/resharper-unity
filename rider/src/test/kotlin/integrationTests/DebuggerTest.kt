package integrationTests

import base.integrationTests.*
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.BeforeMethod
import org.testng.annotations.Test

class DebuggerTest : IntegrationDebuggerTestBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithoutPlugin"

    @BeforeMethod(alwaysRun = true)
    fun replaceNewBehaviourScriptContentIfNeeded() {
        val newBehaviourScript = "NewBehaviourScript.cs"

        if (testCaseSourceDirectory.resolve(newBehaviourScript).exists()) {
            replaceFileContent(project, newBehaviourScript)
            rebuildSolutionWithReSharperBuild()
        }

        selectAttachDebuggerToUnityEditorAndPlayConfiguration()
    }

    @Test
    fun checkUnityPausePoint() {
        val pauseFile = activeSolutionDirectory.combine("Assets", "pause").absoluteFile
        debugUnityProgramWithoutGold(
            {
                toggleUnityPausepoint("NewBehaviourScript.cs", 17, "System.IO.File.Exists(\"${pauseFile.path}\")")
            }
        ) {
            waitForUnityEditorPlaying()
            pauseFile.createNewFile()
            waitForUnityEditorPaused()
            unpause()
            waitForUnityEditorPlaying()
        }
    }

    @Test
    fun checkBreakpoint() {
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

    @Test(description = "RIDER-24651")
    fun checkExceptionBreakpointWithJustMyCode() {
        debugUnityProgramWithGold(testGoldFile,
            {
                toggleExceptionBreakpoint("System.Exception").justMyCode = true
            }
        ) {
            waitForPause()
            dumpFullCurrentData()
            resumeSession()
        }
    }
}