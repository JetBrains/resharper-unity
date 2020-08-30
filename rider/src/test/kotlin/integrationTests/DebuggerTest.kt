package integrationTests

import base.integrationTests.*
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.scriptingApi.*
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS])
class DebuggerTest : IntegrationDebuggerTestBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithoutPlugin"

    override fun preprocessTempDirectory(tempDir: File) {
        super.preprocessTempDirectory(tempDir)

        val newBehaviourScript = "NewBehaviourScript.cs"
        val sourceScript = testCaseSourceDirectory.resolve(newBehaviourScript)
        if (sourceScript.exists()) {
            sourceScript.copyTo(tempDir.resolve("Assets").resolve(newBehaviourScript), true)
        }
    }

    @Test
    fun checkUnityPausePoint() {
        val pauseFile = activeSolutionDirectory.combine("Assets", ".pause").absoluteFile
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleUnityPausepoint("NewBehaviourScript.cs", 16, "System.IO.File.Exists(\"${pauseFile.path}\")")
            },
            {
                waitForPauseModeAfterAction { pauseFile.createNewFile() }
                unpause()
            })
    }

    @Test
    fun checkBreakpoint() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint("NewBehaviourScript.cs", 17)
            },
            {
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "RIDER-24651")
    fun checkExceptionBreakpointWithJustMyCode() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleExceptionBreakpoint("System.Exception").justMyCode = true
            },
            {
                waitForPause()
                dumpFullCurrentData()
                resumeSession()
            }, testGoldFile)
    }

    @Test(description = "RIDER-23087")
    fun checkEvaluationAfterRestartGame() {
        attachDebuggerToUnityEditorAndPlay(
            {
                toggleBreakpoint(project, "NewBehaviourScript.cs", 17)
            },
            {
                val toEvaluate = "binaryNotation / 25"
                fun action() {
                    waitForPause()
                    printlnIndented("$toEvaluate = ${evaluateExpression(toEvaluate).result}")
                    dumpFullCurrentData()
                    resumeSession()
                }

                action()
                restart()
                action()
            }, testGoldFile)
    }
}