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

    @Test
    fun checkPlayingPauseModesAndSteps() {
        play()
        pause()
        step()
        unpause()
        stopPlaying()
    }

    @Test
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

    @Test
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

    @Test
    fun checkPlayModeLogs() {
        replaceFileContent(project, "NewBehaviourScript.cs")
        rebuildSolutionWithReSharperBuild()
        refreshUnityModel()

        waitForEditorLogsAfterAction("Start", "StartFromBackgroundThread") { play() }
        pause()
        waitForEditorLogsAfterAction("Update", "UpdateFromBackgroundThread") { step(false) }
        unpause()
        waitForEditorLogsAfterAction("Quit", "QuitFromBackgroundThread") { stopPlaying() }
    }
}