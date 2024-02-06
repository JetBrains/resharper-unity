package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.scriptingApi.rebuildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.Test
import java.io.File
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.unity.test.framework.UnityVersion

abstract class PlayModeTestBase(private val unityVersion: UnityVersion) : IntegrationTestWithUnityProjectBase() {
    override fun getSolutionDirectoryName() = "UnityDebugAndUnitTesting/Project"
    override val unityMajorVersion = this.unityVersion

    override val testClassDataDirectory: File
        get() = super.testClassDataDirectory.parentFile.combine(PlayModeTestBase::class.simpleName!!)
    override val testCaseSourceDirectory: File
        get() = testClassDataDirectory.combine(super.testStorage.testMethod.name)

    @Test(description="Check play, pause, step, unpause, stop actions for Unity")
    fun checkPlayingPauseModesAndSteps() {
        play()
        pause()
        step()
        unpause()
        stopPlaying()
    }

    @Test(description="Check play, pause, step, unpause, stop actions for Unity with Attach to Unity Process")
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

    @Test(description="Check play, pause, step, unpause, stop actions for Unity with Attach to Unity Process and Play")
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

    @Test(description="Check start, update, quit logs")
    fun checkPlayModeLogs() {
        replaceFileContent(project, "NewBehaviourScript.cs",)
        rebuildSolutionWithReSharperBuild()
        refreshUnityModel()

        waitForEditorLogsAfterAction("Start", "StartFromBackgroundThread") { play() }
        pause()
        waitForEditorLogsAfterAction("Update", "UpdateFromBackgroundThread") { step(false) }
        unpause()
        waitForEditorLogsAfterAction("Quit", "QuitFromBackgroundThread") { stopPlaying() }
    }
}