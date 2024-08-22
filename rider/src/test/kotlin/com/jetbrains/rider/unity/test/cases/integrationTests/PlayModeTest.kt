package com.jetbrains.rider.unity.test.cases.integrationTests

import com.jetbrains.rider.test.annotations.*
import com.jetbrains.rider.test.enums.PlatformType
import com.jetbrains.rider.test.framework.combine
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.scriptingApi.rebuildSolutionWithReSharperBuild
import com.jetbrains.rider.test.scriptingApi.replaceFileContent
import com.jetbrains.rider.unity.test.framework.EngineVersion
import com.jetbrains.rider.unity.test.framework.Tuanjie
import com.jetbrains.rider.unity.test.framework.Unity
import com.jetbrains.rider.unity.test.framework.api.*
import com.jetbrains.rider.unity.test.framework.base.IntegrationTestWithUnityProjectBase
import org.testng.annotations.Test
import java.io.File

@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("PlayMode Action for Unity")
@Severity(SeverityLevel.CRITICAL)
@TestEnvironment(platform = [PlatformType.WINDOWS_ALL, PlatformType.MAC_OS_ALL])
@Suppress("unused")
abstract class PlayModeTest(engineVersion: EngineVersion) : IntegrationTestWithUnityProjectBase(engineVersion) {
    override val testSolution
        get() = if (engineVersion.isTuanjie()) "TuanjieDebugAndUnitTesting/Project"
        else "UnityDebugAndUnitTesting/Project"

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

class PlayModeTestUnity2020 : PlayModeTest(Unity.V2020)
class PlayModeTestUnity2022 : PlayModeTest(Unity.V2022) {
    init {
        addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
    }
}

class PlayModeTestUnity2023 : PlayModeTest(Unity.V2023) {
    init {
        addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
    }
}

class PlayModeTestUnity6 : PlayModeTest(Unity.V6)
{
    init {
        addMute(Mute("RIDER-105666"), ::checkPlayModeLogs)
    }
}
@Mute("RIDER-113191")
class PlayModeTestTuanjie2022 : PlayModeTest(Tuanjie.V2022)