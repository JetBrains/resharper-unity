package integrationTests

import base.integrationTests.*
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.rebuildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import org.testng.annotations.Test

@TestEnvironment(platform = [PlatformType.WINDOWS, PlatformType.MAC_OS])
class PlayModeTest : IntegrationTestWithEditorBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithoutPlugin"

    @Test(enabled = true)
    fun checkPlayingPauseModesAndSteps() {
        play()
        pause()
        step()
        unpause()
        stopPlaying()
    }

    @Test(enabled = true)
    fun checkAttachDebuggerToUnityEditor() {
        attachDebuggerToUnityEditor({},
            {
                play()
                pause()
                step()
                unpause()
                stopPlaying()
            })
    }

    @Test(enabled = true)
    fun checkAttachDebuggerToUnityEditorAndPlay() {
        attachDebuggerToUnityEditorAndPlay({},
            {
                waitForUnityEditorPlayMode()
                pause()
                step()
                unpause()
                stopPlaying()
            })
    }

    @Test(enabled = true)
    fun checkPlayModeLogs() {
        replaceFileContent(project, "NewBehaviourScript.cs")
        rebuildSolutionWithReSharperBuild()
        refreshUnityModel()

        waitForEditorLogAfterAction("Start") { play() }
        pause()
        step("Update")
        unpause()
        waitForEditorLogAfterAction("Quit") { stopPlaying() }
    }
}