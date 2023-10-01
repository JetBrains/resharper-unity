package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.allure.Subsystem
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.scriptingApi.rebuildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithGeneratedSolutionBase
import io.qameta.allure.Description
import io.qameta.allure.Epic
import io.qameta.allure.Feature
import io.qameta.allure.Severity
import io.qameta.allure.SeverityLevel
import org.testng.annotations.Test

@Epic(Subsystem.UNITY_PLUGIN)
@Feature("PlayMode Action for Unity")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
class PlayModeTest : IntegrationTestWithGeneratedSolutionBase() {
    override fun getSolutionDirectoryName() = "SimpleUnityProjectWithoutPlugin"

    @Test(enabled = false)
    @Description("Check play, pause, step, unpause, stop actions for Unity")
    fun checkPlayingPauseModesAndSteps() {
        play()
        pause()
        step()
        unpause()
        stopPlaying()
    }

    @Test(enabled = false)
    @Description("Check play, pause, step, unpause, stop actions for Unity with Attach to Unity Process")
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
    @Description("Check play, pause, step, unpause, stop actions for Unity with Attach to Unity Process and Play")
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
    @Description("Check start, update, quit logs")
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