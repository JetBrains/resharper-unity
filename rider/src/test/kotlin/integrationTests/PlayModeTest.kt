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

    @Test(enabled = false)
    fun checkPlayingPauseModesAndSteps() {
        play()
        pause()
        step()
        unpause()
        stopPlaying()
    }

    @Test(enabled = false)
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

    @Test(enabled = false)
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

    @Test(enabled = false)
    fun checkPlayModeLogs() {
        replaceFileContent(project, "NewBehaviourScript.cs")
        rebuildSolutionWithReSharperBuild()
        refreshUnityModel()

        play()
        waitForEditorLogAfterAction("Start") { play() }
        pause()
        step("Update")
        unpause()
        waitForEditorLogAfterAction("Quit") { stopPlaying() }
    }
}