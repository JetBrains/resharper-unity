package integrationTests

import base.integrationTests.*
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
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
}